//Apache2, 2017-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev

using System;
using System.IO;
namespace Typography.OpenFont.Tables
{
    /// <summary>
    /// hhea
    /// </summary>
    class HorizontalHeader : TableEntry
    {
        public const string _N = "hhea";
        public override string Name => _N;

        //https://docs.microsoft.com/en-us/typography/opentype/spec/hhea
        //hhea — Horizontal Header Table
        //-----
        // Type     Name                    Description
        //uint16    majorVersion            Major version number of the horizontal header table — set to 1.
        //uint16    minorVersion            Minor version number of the horizontal header table — set to 0.
        //FWORD     Ascender                Typographic ascent(Distance from baseline of highest ascender).
        //FWORD     Descender               Typographic descent(Distance from baseline of lowest descender).
        //FWORD     LineGap                 Typographic line gap.
        //                                  Negative  LineGap values are treated as zero in Windows 3.1, and in Mac OS System 6 and System 7.
        //UFWORD    advanceWidthMax         Maximum advance width value in 'hmtx' table.
        //FWORD     minLeftSideBearing      Minimum left sidebearing value in 'hmtx' table.
        //FWORD     minRightSideBearing     Minimum right sidebearing value; calculated as Min(aw - lsb - (xMax - xMin)).
        //FWORD     xMaxExtent              Max(lsb + (xMax - xMin)).
        //int16     caretSlopeRise          Used to calculate the slope of the cursor(rise/run); 1 for vertical.
        //int16     caretSlopeRun 	        0 for vertical.
        //int16     caretOffset             The amount by which a slanted highlight on a glyph needs to be shifted to produce the best appearance.Set to 0 for non-slanted fonts
        //int16     (reserved)              set to 0
        //int16     (reserved)              set to 0
        //int16     (reserved)              set to 0
        //int16     (reserved)              set to 0
        //int16 metricDataFormat 	        0 for current format.
        //uint16  numberOfHMetrics          Number of hMetric entries in 'hmtx' table

        public HorizontalHeader()
        {
        }
        protected override void ReadContentFrom(BinaryReader input)
        {
            Version = input.ReadUInt32(); //major + minor
            Ascent = input.ReadInt16();
            Descent = input.ReadInt16();
            LineGap = input.ReadInt16();

            AdvancedWidthMax = input.ReadUInt16();
            MinLeftSideBearing = input.ReadInt16();
            MinRightSideBearing = input.ReadInt16();
            MaxXExtent = input.ReadInt16();

            CaretSlopRise = input.ReadInt16();
            CaretSlopRun = input.ReadInt16();
            CaretOffset = input.ReadInt16();

            //reserve 4 int16 fields, int16 x 4 fields
            input.BaseStream.Seek(2 * 4, SeekOrigin.Current);

            MetricDataFormat = input.ReadInt16(); // 0
            NumberOfHMetrics = input.ReadUInt16();
        }
        public uint Version { get; private set; }
        public short Ascent { get; private set; }
        public short Descent { get; private set; }
        public short LineGap { get; private set; }
        public ushort AdvancedWidthMax { get; private set; }
        public short MinLeftSideBearing { get; private set; }
        public short MinRightSideBearing { get; private set; }
        public short MaxXExtent { get; private set; }
        public short CaretSlopRise { get; private set; }
        public short CaretSlopRun { get; private set; }
        public short CaretOffset { get; private set; }
        public short MetricDataFormat { get; private set; }
        public ushort NumberOfHMetrics { get; private set; }
    }
}
