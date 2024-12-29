//Apache2, 2016-present, WinterDev
using System;
using System.IO;
namespace Typography.OpenFont.Tables
{
    /// <summary>
    /// Grid-fitting And Scan-conversion Procedure Table
    /// </summary>
    class Gasp : TableEntry
    {
        public const string _N = "gasp";
        public override string Name => _N;
        //
        //https://docs.microsoft.com/en-us/typography/opentype/spec/gasp


        // This table contains information which describes the preferred rasterization techniques 
        //for the typeface when it is rendered on grayscale-capable devices. 
        //This table also has some use for monochrome devices,
        //which may use the table to turn off hinting at very large or small sizes, to improve performance.

        //At very small sizes, 
        //the best appearance on grayscale devices can usually be achieved by rendering the glyphs 
        //in grayscale without using hints. 
        //
        //At intermediate sizes, hinting and monochrome rendering will usually produce the best appearance. 
        //
        //At large sizes, the combination of hinting and grayscale rendering will
        //typically produce the best appearance.

        //If the 'gasp' table is not present in a typeface,
        //the rasterizer may apply default rules to decide how to render the glyphs on grayscale devices.

        //The 'gasp' table consists of a header followed by groupings of 'gasp' records:
        GaspRangeRecord[] _rangeRecords;
        protected override void ReadContentFrom(BinaryReader reader)
        {

            //Type 	        Name 	            Description
            //USHORT 	    version 	        Version number (set to 1)
            //USHORT 	    numRanges 	        Number of records to follow
            //GASPRANGE     gaspRange[numRanges] 	Sorted by ppem

            //Each GASPRANGE record looks like this:
            //Type 	        Name 	            Description
            //USHORT 	    rangeMaxPPEM 	    Upper limit of range, in PPEM
            //USHORT 	    rangeGaspBehavior 	Flags describing desired rasterizer behavior.
            ushort version = reader.ReadUInt16();
            ushort numRanges = reader.ReadUInt16();
            _rangeRecords = new GaspRangeRecord[numRanges];
            for (int i = 0; i < numRanges; ++i)
            {
                _rangeRecords[i] = new GaspRangeRecord(
                    reader.ReadUInt16(),
                    (GaspRangeBehavior)reader.ReadUInt16());
            }
        }

        [Flags]
        enum GaspRangeBehavior : ushort
        {
            Neither = 0,
            GASP_DOGRAY = 0x0002,
            GASP_GRIDFIT = 0x0001,
            GASP_DOGRAY_GASP_GRIDFIT = 0x0003,
            GASP_SYMMETRIC_GRIDFIT = 0x0004,
            GASP_SYMMETRIC_SMOOTHING = 0x0008,
            GASP_SYMMETRIC_SMOOTHING_GASP_SYMMETRIC_GRIDFIT = 0x000C
        }
        readonly struct GaspRangeRecord
        {
            public readonly ushort rangeMaxPPEM;
            public readonly GaspRangeBehavior rangeGaspBehavior;
            public GaspRangeRecord(ushort rangeMaxPPEM, GaspRangeBehavior rangeGaspBehavior)
            {
                this.rangeMaxPPEM = rangeMaxPPEM;
                this.rangeGaspBehavior = rangeGaspBehavior;
            }

            // There are four flags for the rangeGaspBehavior flags:
            //Flag 	Meaning
            //GASP_DOGRAY 	Use grayscale rendering
            //GASP_GRIDFIT 	Use gridfitting
            //GASP_SYMMETRIC_SMOOTHING 	Use smoothing along multiple axes with ClearType®
            //Only supported in version 1 gasp
            //GASP_SYMMETRIC_GRIDFIT 	Use gridfitting with ClearType symmetric smoothing
            //Only supported in version 1 gasp

            //The set of bit flags may be extended in the future. 
            //The first two bit flags operate independently of the following two bit flags.
            //If font smoothing is enabled, then the first two bit flags are used. 
            //If ClearType is enabled, then the following two bit flags are used. The seven currently defined values of rangeGaspBehavior would have the following uses:
            //Flag 	Value 	Meaning

            //GASP_DOGRAY 	0x0002 	small sizes, typically ppem<9
            //GASP_GRIDFIT 	0x0001 	medium sizes, typically 9<=ppem<=16
            //GASP_DOGRAY|GASP_GRIDFIT 	0x0003 	large sizes, typically ppem>16
            //(neither) 	0x0000 	optional for very large sizes, typically ppem>2048
            //GASP_SYMMETRIC_GRIDFIT 	0x0004 	typically always enabled
            //GASP_SYMMETRIC_SMOOTHING 	0x0008 	larger screen sizes, typically ppem>15, most commonly used with the gridfit flag.
            //GASP_SYMMETRIC_SMOOTHING| GASP_SYMMETRIC_GRIDFIT 	0x000C 	larger screen sizes, typically ppem>15
            //neither 	0x0000 	optional for very large sizes, typically ppem>2048
        }
    }

}