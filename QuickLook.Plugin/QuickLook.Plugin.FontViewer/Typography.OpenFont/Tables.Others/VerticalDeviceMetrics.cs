//Apache2, 2016-present, WinterDev 

using System.IO;

namespace Typography.OpenFont.Tables
{
    class VerticalDeviceMetrics : TableEntry
    {
        public const string _N = "VDMX";
        public override string Name => _N;
        //
        //https://docs.microsoft.com/en-us/typography/opentype/spec/vdmx
        //VDMX - Vertical Device Metrics 
        //The VDMX table relates to OpenType™ fonts with TrueType outlines.
        //Under Windows, the usWinAscent and usWinDescent values from the 'OS/2' table
        //will be used to determine the maximum black height for a font at any given size.
        //Windows calls this distance the Font Height.
        //Because TrueType instructions can lead to Font Heights that differ from the actual scaled and rounded values,
        //basing the Font Height strictly on the yMax and yMin can result in “lost pixels.” 
        //Windows will clip any pixels that extend above the yMax or below the yMin. 
        //In order to avoid grid fitting the entire font to determine the correct height, the VDMX table has been defined.

        //The VDMX table consists of a header followed by groupings of VDMX records:
        Ratio[] _ratios;
        protected override void ReadContentFrom(BinaryReader reader)
        {
            //uint16 	version 	Version number (0 or 1).
            //uint16 	numRecs 	Number of VDMX groups present
            //uint16 	numRatios 	Number of aspect ratio groupings
            //RatioRange 	ratRange[numRatios] 	Ratio ranges (see below for more info)
            //Offset16 	offset[numRatios] 	Offset from start of this table to the VDMX group for this ratio range.
            //---
            //RatioRange Record:
            //Type  	Name 	        Description
            //uint8 	bCharSet 	    Character set (see below).
            //uint8 	xRatio 	        Value to use for x-Ratio
            //uint8 	yStartRatio 	Starting y-Ratio value.
            //uint8 	yEndRatio 	    Ending y-Ratio value.
            ushort version = reader.ReadUInt16();
            ushort numRecs = reader.ReadUInt16();
            ushort numRatios = reader.ReadUInt16();
            _ratios = new Ratio[numRatios];
            for (int i = 0; i < numRatios; ++i)
            {
                _ratios[i] = new Ratio(
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte());
            }
            ushort[] offsets = Utils.ReadUInt16Array(reader, numRatios);
            //------
            //actual vdmx group
            //TODO: implement this
        }
        readonly struct Ratio
        {
            public readonly byte charset;
            public readonly byte xRatio;
            public readonly byte yStartRatio;
            public readonly byte yEndRatio;
            public Ratio(byte charset, byte xRatio, byte yStartRatio, byte yEndRatio)
            {
                this.charset = charset;
                this.xRatio = xRatio;
                this.yStartRatio = yStartRatio;
                this.yEndRatio = yEndRatio;
            }
        }
    }
}
