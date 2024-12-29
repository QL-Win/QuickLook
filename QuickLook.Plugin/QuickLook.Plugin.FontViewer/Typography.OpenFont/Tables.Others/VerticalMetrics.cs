//Apache2, 2017-present, WinterDev

using System;
using System.IO;
namespace Typography.OpenFont.Tables
{
    /// <summary>
    /// vertical metrics table
    /// </summary>
    class VerticalMetrics : TableEntry
    {
        public const string _N = "vmtx";
        public override string Name => _N;

        // https://docs.microsoft.com/en-us/typography/opentype/spec/vmtx
        // vmtx - Vertical Metrics Table

        //The vertical metrics table allows you to specify the vertical spacing for each glyph in a vertical font.
        //This table consists of either one or two arrays that contain metric information(the advance heights and top sidebearings)
        //for the vertical layout of each of the glyphs in the font.
        //The vertical metrics coordinate system is shown below.


        //Vertical Metrics Table Format

        //The overall structure of the vertical metrics table consists of two arrays shown below:
        //the vMetrics array followed by an array of top side bearings.
        //
        //The top side bearing is measured relative to the top of the origin of glyphs, 
        //for vertical composition of ideographic glyphs.
        //       
        //This table does not have a header, 
        //but does require that the number of glyphs included in the two arrays equals the total number of glyphs in the font.
        //
        //The number of entries in the vMetrics array is determined by the value of the numOfLongVerMetrics field of the vertical header table.
        //
        //The vMetrics array contains two values for each entry.
        //These are the advance height and the top sidebearing for each glyph included in the array.
        //
        //In monospaced fonts, such as Courier or Kanji, all glyphs have the same advance height.
        //If the font is monospaced, only one entry need be in the first array, but that one entry is required.
        //The format of an entry in the vertical metrics array is given below.

        //
        //Type      Name            Description
        //uint16    advanceHeight   The advance height of the glyph. Unsigned integer in FUnits
        //int16     topSideBearing  The top sidebearing of the glyph. Signed integer in FUnits.

        //The second array is optional and generally is used for a run of monospaced glyphs in the font.
        //Only one such run is allowed per font, and it must be located at the end of the font.
        //This array contains the top sidebearings of glyphs not represented in the first array,
        //and all the glyphs in this array must have the same advance height as the last entry in the vMetrics array.
        //All entries in this array are therefore monospaced.
        //
        //The number of entries in this array is calculated by subtracting the value of numOfLongVerMetrics from the number of glyphs in the font.
        //The sum of glyphs represented in the first array plus the glyphs represented in the second array therefore equals the number of glyphs in the font.
        //The format of the top sidebearing array is given below.
        //Type      Name                Description
        // int16    topSideBearing[]    The top sidebearing of the glyph. Signed integer in FUnits.

        ushort _numOfLongVerMetrics;
        AdvanceHeightAndTopSideBearing[] _advHeightAndTopSideBearings;
        public VerticalMetrics(ushort numOfLongVerMetrics)
        {
            _numOfLongVerMetrics = numOfLongVerMetrics;
        }
        protected override void ReadContentFrom(BinaryReader reader)
        {
            _advHeightAndTopSideBearings = new AdvanceHeightAndTopSideBearing[_numOfLongVerMetrics];
            int m = 0;
            for (int i = _numOfLongVerMetrics - 1; i >= 0; --i)
            {
                _advHeightAndTopSideBearings[m] = new AdvanceHeightAndTopSideBearing(
                    reader.ReadUInt16(),
                    reader.ReadInt16()
                    );
            }
        }

        public readonly struct AdvanceHeightAndTopSideBearing
        {
            public readonly ushort advanceHeight;
            public readonly short topSideBearing;
            public AdvanceHeightAndTopSideBearing(ushort advanceHeight, short topSideBearing)
            {
                this.advanceHeight = advanceHeight;
                this.topSideBearing = topSideBearing;
            }
#if DEBUG
            public override string ToString()
            {
                return advanceHeight + "," + topSideBearing;
            }
#endif
        }

    }
}