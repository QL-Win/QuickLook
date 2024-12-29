//Apache2, 2017-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev

using System;
using System.IO;
namespace Typography.OpenFont.Tables
{

    class Head : TableEntry
    {

        //https://docs.microsoft.com/en-us/typography/opentype/spec/head
        //This table gives global information about the font. 
        //The bounding box values should be computed using only glyphs that have contours.
        //Glyphs with no contours should be ignored for the purposes of these calculations.

        public const string _N = "head";
        public override string Name => _N;
        //
        short _indexToLocFormat;

        public Head()
        {
        }
        protected override void ReadContentFrom(BinaryReader input)
        {

            //Type 	    Name 	        Description
            //uint16 	majorVersion 	Major version number of the font header table — set to 1.
            //uint16 	minorVersion 	Minor version number of the font header table — set to 0.
            //Fixed 	fontRevision 	Set by font manufacturer.
            //uint32 	checkSumAdjustment 	To compute: set it to 0, sum the entire font as uint32, then store 0xB1B0AFBA - sum.
            //                          If the font is used as a component in a font collection file, 
            //                          the value of this field will be invalidated by changes to the file structure and font table directory, and must be ignored.
            //uint32 	magicNumber 	Set to 0x5F0F3CF5.
            //uint16 	flags 	        Bit 0: Baseline for font at y=0;

            //                          Bit 1: Left sidebearing point at x=0 (relevant only for TrueType rasterizers) — see the note below regarding variable fonts;

            //                          Bit 2: Instructions may depend on point size;

            //                          Bit 3: Force ppem to integer values for all internal scaler math; may use fractional ppem sizes if this bit is clear;

            //                          Bit 4: Instructions may alter advance width (the advance widths might not scale linearly);

            //                          Bit 5: This bit is not used in OpenType, and should not be set in order to ensure compatible behavior on all platforms. If set, it may result in different behavior for vertical layout in some platforms. (See Apple’s specification for details regarding behavior in Apple platforms.)

            //                          Bits 6–10: These bits are not used in Opentype and should always be cleared. (See Apple’s specification for details regarding legacy used in Apple platforms.)

            //                          Bit 11: Font data is “lossless” as a result of having been subjected to optimizing transformation and/or compression (such as e.g. compression mechanisms defined by ISO/IEC 14496-18, MicroType Express, WOFF 2.0 or similar) where the original font functionality and features are retained but the binary compatibility between input and output font files is not guaranteed. As a result of the applied transform, the DSIG table may also be invalidated.

            //                          Bit 12: Font converted (produce compatible metrics)

            //                          Bit 13: Font optimized for ClearType™. Note, fonts that rely on embedded bitmaps (EBDT) for rendering should not be considered optimized for ClearType, and therefore should keep this bit cleared.

            //                          Bit 14: Last Resort font. If set, indicates that the glyphs encoded in the 'cmap' subtables are simply generic symbolic representations of code point ranges and don’t truly represent support for those code points. If unset, indicates that the glyphs encoded in the 'cmap' subtables represent proper support for those code points.

            //                          Bit 15: Reserved, set to 0
            //uint16 	    unitsPerEm 	Set to a value from 16 to 16384. Any value in this range is valid.
            //                          In fonts that have TrueType outlines, a power of 2 is recommended as this allows performance optimizations in some rasterizers.
            //LONGDATETIME 	created 	Number of seconds since 12:00 midnight that started January 1st 1904 in GMT/UTC time zone. 64-bit integer
            //LONGDATETIME 	modified 	Number of seconds since 12:00 midnight that started January 1st 1904 in GMT/UTC time zone. 64-bit integer
            //int16 	xMin 	        For all glyph bounding boxes.
            //int16 	yMin 	        For all glyph bounding boxes.
            //int16 	xMax 	        For all glyph bounding boxes.
            //int16 	yMax 	        For all glyph bounding boxes.
            //uint16 	macStyle 	    Bit 0: Bold (if set to 1);
            //                          Bit 1: Italic (if set to 1)
            //                          Bit 2: Underline (if set to 1)
            //                          Bit 3: Outline (if set to 1)
            //                          Bit 4: Shadow (if set to 1)
            //                          Bit 5: Condensed (if set to 1)
            //                          Bit 6: Extended (if set to 1)
            //                          Bits 7–15: Reserved (set to 0).
            //uint16 	lowestRecPPEM 	Smallest readable size in pixels.
            //int16 	fontDirectionHint 	Deprecated (Set to 2).
            //                          0: Fully mixed directional glyphs;
            //                          1: Only strongly left to right;
            //                          2: Like 1 but also contains neutrals;
            //                          -1: Only strongly right to left;
            //                          -2: Like -1 but also contains neutrals.

            //(A neutral character has no inherent directionality; it is not a character with zero (0) width. Spaces and punctuation are examples of neutral characters. Non-neutral characters are those with inherent directionality. For example, Roman letters (left-to-right) and Arabic letters (right-to-left) have directionality. In a “normal” Roman font where spaces and punctuation are present, the font direction hints should be set to two (2).)
            //int16 	indexToLocFormat 	0 for short offsets (Offset16), 1 for long (Offset32).
            //int16 	glyphDataFormat 	0 for current format.


            Version = input.ReadUInt32(); // 0x00010000 for version 1.0.
            FontRevision = input.ReadUInt32();
            CheckSumAdjustment = input.ReadUInt32();
            MagicNumber = input.ReadUInt32();
            if (MagicNumber != 0x5F0F3CF5) throw new Exception("Invalid magic number!" + MagicNumber.ToString("x"));

            Flags = input.ReadUInt16();
            UnitsPerEm = input.ReadUInt16(); // valid is 16 to 16384
            Created = input.ReadUInt64(); //  International date (8-byte field). (?)
            Modified = input.ReadUInt64();
            // bounding box for all glyphs
            Bounds = Utils.ReadBounds(input);
            MacStyle = input.ReadUInt16();
            LowestRecPPEM = input.ReadUInt16();
            FontDirectionHint = input.ReadInt16();
            _indexToLocFormat = input.ReadInt16(); // 0 for 16-bit offsets, 1 for 32-bit.
            GlyphDataFormat = input.ReadInt16(); // 0
        }

        public uint Version { get; private set; }
        public uint FontRevision { get; private set; }
        public uint CheckSumAdjustment { get; private set; }
        public uint MagicNumber { get; private set; }
        public ushort Flags { get; private set; }
        public ushort UnitsPerEm { get; private set; }
        public ulong Created { get; private set; }
        public ulong Modified { get; private set; }
        public Bounds Bounds { get; private set; }
        public ushort MacStyle { get; private set; }
        public ushort LowestRecPPEM { get; private set; }
        public short FontDirectionHint { get; private set; }
        public bool WideGlyphLocations => _indexToLocFormat > 0;
        public short GlyphDataFormat { get; private set; }
    }
}
