//Apache2, 2017-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev


using System.IO;
namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/maxp
    class MaxProfile : TableEntry
    {
        public const string _N = "maxp";
        public override string Name => _N;

        //This table establishes the memory requirements for this font.
        //Fonts with CFF data must use Version 0.5 of this table,
        //specifying only the numGlyphs field.

        //Fonts with TrueType outlines must use Version 1.0 of this table,  where all data is required.

        //Version 0.5
        //Type      Name        Description
        //Fixed     version 	0x00005000 for version 0.5
        //                      (Note the difference in the representation of a non-zero fractional part, in Fixed numbers.)
        //uint16 numGlyphs      The number of glyphs in the font.

        //Version 1.0
        //Type      Name                Description
        //Fixed     version 	        0x00010000 for version 1.0.
        //uint16    numGlyphs           The number of glyphs in the font.
        //uint16    maxPoints           Maximum points in a non-composite glyph.
        //uint16    maxContours         Maximum contours in a non-composite glyph.
        //uint16    maxCompositePoints  Maximum points in a composite glyph.
        //uint16    maxCompositeContours    Maximum contours in a composite glyph.
        //uint16    maxZones 	        1 if instructions do not use the twilight zone (Z0), or 2 if instructions do use Z0; should be set to 2 in most cases.
        //uint16    maxTwilightPoints   Maximum points used in Z0.
        //uint16    maxStorage          Number of Storage Area locations.
        //uint16    maxFunctionDefs     Number of FDEFs, equal to the highest function number + 1.
        //uint16    maxInstructionDefs  Number of IDEFs.
        //uint16    maxStackElements    Maximum stack depth across Font Program ('fpgm' table), CVT Program('prep' table) and all glyph instructions(in the 'glyf' table).
        //uint16    maxSizeOfInstructions   Maximum byte count for glyph instructions.
        //uint16    maxComponentElements    Maximum number of components referenced at “top level” for any composite glyph.
        //uint16    maxComponentDepth       Maximum levels of recursion; 1 for simple components.

        public uint Version { get; private set; }
        public ushort GlyphCount { get; private set; }
        public ushort MaxPointsPerGlyph { get; private set; }
        public ushort MaxContoursPerGlyph { get; private set; }
        public ushort MaxPointsPerCompositeGlyph { get; private set; }
        public ushort MaxContoursPerCompositeGlyph { get; private set; }
        public ushort MaxZones { get; private set; }
        public ushort MaxTwilightPoints { get; private set; }
        public ushort MaxStorage { get; private set; }
        public ushort MaxFunctionDefs { get; private set; }
        public ushort MaxInstructionDefs { get; private set; }
        public ushort MaxStackElements { get; private set; }
        public ushort MaxSizeOfInstructions { get; private set; }
        public ushort MaxComponentElements { get; private set; }
        public ushort MaxComponentDepth { get; private set; }

        protected override void ReadContentFrom(BinaryReader input)
        {
            Version = input.ReadUInt32(); // 0x00010000 == 1.0
            GlyphCount = input.ReadUInt16();
            MaxPointsPerGlyph = input.ReadUInt16();
            MaxContoursPerGlyph = input.ReadUInt16();
            MaxPointsPerCompositeGlyph = input.ReadUInt16();
            MaxContoursPerCompositeGlyph = input.ReadUInt16();
            MaxZones = input.ReadUInt16();
            MaxTwilightPoints = input.ReadUInt16();
            MaxStorage = input.ReadUInt16();
            MaxFunctionDefs = input.ReadUInt16();
            MaxInstructionDefs = input.ReadUInt16();
            MaxStackElements = input.ReadUInt16();
            MaxSizeOfInstructions = input.ReadUInt16();
            MaxComponentElements = input.ReadUInt16();
            MaxComponentDepth = input.ReadUInt16();
        }
    }
}
