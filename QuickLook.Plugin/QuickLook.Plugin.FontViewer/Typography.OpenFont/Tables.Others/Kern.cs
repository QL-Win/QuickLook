//Apache2, 2017-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev

using System.Collections.Generic;
using System.IO;
namespace Typography.OpenFont.Tables
{
    class Kern : TableEntry
    {
        public const string _N = "kern";
        public override string Name => _N;
        // 
        //https://docs.microsoft.com/en-us/typography/opentype/spec/kern

        //Note: Apple has extended the definition of the 'kern' table to provide additional functionality.
        //The Apple extensions are not supported on Windows.Fonts intended for cross-platform use or 
        //for the Windows platform in general should conform to the 'kern' table format specified here.

        List<KerningSubTable> _kernSubTables = new List<KerningSubTable>();
        public short GetKerningDistance(ushort left, ushort right)
        {
            //use kern sub table 0
            //TODO: review if have more than 1 table
            return _kernSubTables[0].GetKernDistance(left, right);
        }
        protected override void ReadContentFrom(BinaryReader reader)
        {
            ushort verion = reader.ReadUInt16();
            ushort nTables = reader.ReadUInt16();//subtable count
            //TODO: review here
            if (nTables > 1)
            {
                Utils.WarnUnimplemented("Support for {0} kerning tables", nTables);
            }

            for (int i = 0; i < nTables; ++i)
            {
                ushort subTableVersion = reader.ReadUInt16();
                ushort len = reader.ReadUInt16(); //Length of the subtable, in bytes (including this header).
                KernCoverage kerCoverage = new KernCoverage(reader.ReadUInt16());//What type of information is contained in this table.

                //The coverage field is divided into the following sub-fields, with sizes given in bits:
                //----------------------------------------------
                //Format of the subtable.
                //Only formats 0 and 2 have been defined.
                //Formats 1 and 3 through 255 are reserved for future use.

                switch (kerCoverage.format)
                {
                    case 0:
                        ReadSubTableFormat0(reader, len - (3 * 2));//3 header field * 2 byte each
                        break;
                    case 2:
                    //TODO: implement
                    default:
                        Utils.WarnUnimplemented("Kerning Coverage Format {0}", kerCoverage.format);
                        break;
                }
            }
        }

        void ReadSubTableFormat0(BinaryReader reader, int remainingBytes)
        {
            ushort npairs = reader.ReadUInt16();
            ushort searchRange = reader.ReadUInt16();
            ushort entrySelector = reader.ReadUInt16();
            ushort rangeShift = reader.ReadUInt16();
            //----------------------------------------------  
            var ksubTable = new KerningSubTable(npairs);
            _kernSubTables.Add(ksubTable);
            while (npairs > 0)
            {
                ksubTable.AddKernPair(
                    reader.ReadUInt16(), //left//
                    reader.ReadUInt16(),//right
                    reader.ReadInt16());//value 
                npairs--;
            }
        }
        readonly struct KerningPair
        {
            /// <summary>
            /// left glyph index
            /// </summary>
            public readonly ushort left;
            /// <summary>
            /// right glyph index
            /// </summary>
            public readonly ushort right;
            /// <summary>
            /// n FUnits. If this value is greater than zero, the characters will be moved apart. If this value is less than zero, the character will be moved closer together.
            /// </summary>
            public readonly short value;
            public KerningPair(ushort left, ushort right, short value)
            {
                this.left = left;
                this.right = right;
                this.value = value;
            }
#if DEBUG
            public override string ToString()
            {
                return left + " " + right;
            }
#endif
        }
        readonly struct KernCoverage
        {
            //horizontal 	0 	1 	1 if table has horizontal data, 0 if vertical.
            //minimum 	1 	1 	If this bit is set to 1, the table has minimum values. If set to 0, the table has kerning values.
            //cross-stream 	2 	1 	If set to 1, kerning is perpendicular to the flow of the text.

            //horizontal ...            
            //If the text is normally written horizontally,
            //kerning will be done in the up and down directions. 
            //If kerning values are positive, the text will be kerned upwards; 
            //if they are negative, the text will be kerned downwards.

            //vertical ...
            //If the text is normally written vertically, 
            //kerning will be done in the left and right directions. 
            //If kerning values are positive, the text will be kerned to the right; 
            //if they are negative, the text will be kerned to the left.

            //The value 0x8000 in the kerning data resets the cross-stream kerning back to 0.
            //override 	3 	1 	If this bit is set to 1 the value in this table should replace the value currently being accumulated.
            //reserved1 	4-7 	4 	Reserved. This should be set to zero.
            //format 	8-15 	8 	Format of the subtable. Only formats 0 and 2 have been defined. Formats 1 and 3 through 255 are reserved for future use.
            //
            public readonly ushort coverage;
            public readonly bool horizontal;
            public readonly bool hasMinimum;
            public readonly bool crossStream;
            public readonly bool _override;
            public readonly byte format;
            public KernCoverage(ushort coverage)
            {
                this.coverage = coverage;
                //bit 0,len 1, 1 if table has horizontal data, 0 if vertical.
                horizontal = (coverage & 0x1) == 1;
                //bit 1,len 1, If this bit is set to 1, the table has minimum values. If set to 0, the table has kerning values.
                hasMinimum = ((coverage >> 1) & 0x1) == 1;
                //bit 2,len 1, If set to 1, kerning is perpendicular to the flow of the text.
                crossStream = ((coverage >> 2) & 0x1) == 1;
                //bit 3,len 1, If this bit is set to 1 the value in this table should replace the value currently being accumulated.
                _override = ((coverage >> 3) & 0x1) == 1;
                //bit 4-7 => 	Reserved. This should be set to zero.
                format = (byte)((coverage >> 8) & 0xff);
            }
        }

        class KerningSubTable
        {
            List<KerningPair> _kernPairs;
            Dictionary<uint, short> _kernDic;
            public KerningSubTable(int capcity)
            {
                _kernPairs = new List<KerningPair>(capcity);
                _kernDic = new Dictionary<uint, short>(capcity);
            }
            public void AddKernPair(ushort left, ushort right, short value)
            {
                _kernPairs.Add(new KerningPair(left, right, value));
                //may has duplicate key ?
                //TODO: review here
                uint key = (uint)((left << 16) | right);
                _kernDic[key] = value; //just replace?                 
            }
            public short GetKernDistance(ushort left, ushort right)
            {
                //find if we have this left & right ?
                uint key = (uint)((left << 16) | right);

                _kernDic.TryGetValue(key, out short found);
                return found;
            }
        }

    }
}