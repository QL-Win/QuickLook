//Apache2, 2017-present, WinterDev
//https://docs.microsoft.com/en-us/typography/opentype/spec/vhea

using System;
using System.IO;
namespace Typography.OpenFont.Tables
{
    class VerticalHeader : TableEntry
    {
        public const string _N = "vhea";
        public override string Name => _N;
        
        //vhea — Vertical Header Tables
        //The vertical header table(tag name: 'vhea') contains information needed for vertical fonts.The glyphs of vertical fonts are written either top to bottom or bottom to top. This table contains information that is general to the font as a whole. Information that pertains to specific glyphs is given in the vertical metrics table (tag name: 'vmtx') described separately.The formats of these tables are similar to those for horizontal metrics (hhea and hmtx).
        //Data in the vertical header table must be consistent with data that appears in the vertical metrics table.The advance height and top sidebearing values in the vertical metrics table must correspond with the maximum advance height and minimum bottom sidebearing values in the vertical header table.
        //See the section “OpenType CJK Font Guidelines“ for more information about constructing CJK (Chinese, Japanese, and Korean) fonts.

        // Table Format

        //The difference between version 1.0 and version 1.1 is the name and definition of the following fields:
        //ascender becomes vertTypoAscender
        //descender becomes vertTypoDescender
        //lineGap becomes vertTypoLineGap
        //
        //Version 1.0 of the vertical header table format is as follows:
        //Version 1.0
        //Type      Name        Description
        //Fixed     version     Version number of the vertical header table; 0x00010000 for version 1.0  
        //int16     ascent      Distance in FUnits from the centerline to the previous line’s descent.
        //int16     descent     Distance in FUnits from the centerline to the next line’s ascent.
        //int16     lineGap     Reserved; set to 0
        //int16     advanceHeightMax    The maximum advance height measurement -in FUnits found in the font.This value must be consistent with the entries in the vertical metrics table.
        //int16     minTop_SideBearing  The minimum top sidebearing measurement found in the font, in FUnits.This value must be consistent with the entries in the vertical metrics table.
        //int16     minBottom_SideBearing  The minimum bottom sidebearing measurement found in the font,in FUnits. 
        //                                 This value must be consistent with the entries in the vertical metrics table.
        //int16     yMaxExtent          Defined as yMaxExtent=minTopSideBearing + (yMax - yMin)
        //int16     caretSlopeRise  The value of the caretSlopeRise field divided by the value of the caretSlopeRun Field determines the slope of the caret. A value of 0 for the rise and a value of 1 for the run specifies a horizontal caret. A value of 1 for the rise and a value of 0 for the run specifies a vertical caret. Intermediate values are desirable for fonts whose glyphs are oblique or italic.For a vertical font, a horizontal caret is best.
        //int16     caretSlopeRun   See the caretSlopeRise field. Value= 1 for nonslanted vertical fonts.
        //int16     caretOffset The amount by which the highlight on a slanted glyph needs to be shifted away from the glyph in order to produce the best appearance. Set value equal to 0 for nonslanted fonts.
        //int16     reserved    Set to 0.
        //int16     reserved    Set to 0.
        //int16     reserved    Set to 0.
        //int16     reserved    Set to 0.
        //int16     metricDataFormat    Set to 0.
        //uint16    numOfLongVerMetrics Number of advance heights in the vertical metrics table.
        //-------------

        // Version 1.1 of the vertical header table format is as follows:
        //Version 1.1
        //Type       Name        Description
        //Fixed     version     Version number of the vertical header table; 0x00011000 for version 1.1 
        //                      Note the representation of a non-zero fractional part, in Fixed numbers.
        //int16     vertTypoAscender    The vertical typographic ascender for this font.It is the distance in FUnits from the ideographic em-box center baseline for the vertical axis to the right of the ideographic em-box and is usually set to (head.unitsPerEm)/2. For example, a font with an em of 1000 fUnits will set this field to 500. See the baseline section of the OpenType Tag Registry for a description of the ideographic em-box.
        //int16     vertTypoDescender   The vertical typographic descender for this font.It is the distance in FUnits from the ideographic em-box center baseline for the horizontal axis to the left of the ideographic em-box and is usually set to (head.unitsPerEm)/2. For example, a font with an em of 1000 fUnits will set this field to 500.
        //int16     vertTypoLineGap     The vertical typographic gap for this font.An application can determine the recommended line spacing for single spaced vertical text for an OpenType font by the following expression: ideo embox width + vhea.vertTypoLineGap
        //
        //int16     advanceHeightMax    The maximum advance height measurement -in FUnits found in the font.This value must be consistent with the entries in the vertical metrics table.
        //int16     minTop_SideBearing  The minimum top sidebearing measurement found in the font, in FUnits.This value must be consistent with the entries in the vertical metrics table.
        //int16     minBottom_SideBearing The minimum bottom sidebearing measurement found in the font,in FUnits.        
        //                               This value must be consistent with the entries in the vertical metrics table.
        //int16     yMaxExtent            Defined as yMaxExtent =minTopSideBearing + (yMax - yMin)
        //int16     caretSlopeRise  The value of the caretSlopeRise field divided by the value of the caretSlopeRun Field determines the slope of the caret.A value of 0 for the rise and a value of 1 for the run specifies a horizontal caret.A value of 1 for the rise and a value of 0 for the run specifies a vertical caret.Intermediate values are desirable for fonts whose glyphs are oblique or italic.For a vertical font, a horizontal caret is best.
        //int16     caretSlopeRun   See the caretSlopeRise field.Value = 1 for nonslanted vertical fonts.
        //int16     caretOffset The amount by which the highlight on a slanted glyph needs to be shifted away from the glyph in order to produce the best appearance.Set value equal to 0 for nonslanted fonts.
        //int16     reserved    Set to 0.
        //int16     reserved    Set to 0.
        //int16     reserved    Set to 0.
        //int16     reserved    Set to 0.
        //int16     metricDataFormat    Set to 0.
        //uint16    numOfLongVerMetrics Number of advance heights in the vertical metrics table.




        public byte VersionMajor { get; set; }
        public byte VersionMinor { get; set; }
        public short VertTypoAscender { get; set; }
        public short VertTypoDescender { get; set; }
        public short VertTypoLineGap { get; set; }
        //
        public short AdvanceHeightMax { get; set; }
        public short MinTopSideBearing { get; set; }
        public short MinBottomSideBearing { get; set; }
        //
        public short YMaxExtend { get; set; }
        public short CaretSlopeRise { get; set; }
        public short CaretSlopeRun { get; set; }
        public short CaretOffset { get; set; }
        public ushort NumOfLongVerMetrics { get; set; }
        protected override void ReadContentFrom(BinaryReader reader)
        {
            uint version = reader.ReadUInt32();
            VersionMajor = (byte)(version >> 16);
            VersionMinor = (byte)(version >> 8);

            VertTypoAscender = reader.ReadInt16();
            VertTypoDescender = reader.ReadInt16();
            VertTypoLineGap = reader.ReadInt16();
            //
            AdvanceHeightMax = reader.ReadInt16();
            MinTopSideBearing = reader.ReadInt16();
            MinBottomSideBearing = reader.ReadInt16();
            //
            YMaxExtend = reader.ReadInt16();
            CaretSlopeRise = reader.ReadInt16();
            CaretSlopeRun = reader.ReadInt16();
            CaretOffset = reader.ReadInt16();
            //
            //skip 5 int16 =>  4 reserve field + 1 metricDataFormat            
            reader.BaseStream.Position += (2 * (4 + 1)); //short = 2 byte, 
            //
            NumOfLongVerMetrics = reader.ReadUInt16();
        }

    }
}
