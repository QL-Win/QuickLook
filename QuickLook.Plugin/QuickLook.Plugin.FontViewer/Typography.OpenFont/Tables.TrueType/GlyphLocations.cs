//MIT, 2018-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev
//https://www.microsoft.com/typography/otspec/loca.htm

using System.IO;
namespace Typography.OpenFont.Tables
{
    class GlyphLocations : TableEntry
    {
        public const string _N = "loca";
        public override string Name => _N;


        // loca - Index to Location

        //The indexToLoc table stores the offsets to the locations of the glyphs in the font,
        //relative to the beginning of the glyphData table.
        //In order to compute the length of the last glyph element, 
        //there is an extra entry after the last valid index.

        //By definition, 
        //index zero points to the “missing character,” 
        //which is the character that appears if a character is not found in the font. 
        //The missing character is commonly represented by a blank box or a space. 
        //If the font does not contain an outline for the missing character, 
        //then the first and second offsets should have the same value.
        //This also applies to any other characters without an outline, such as the space character.
        //If a glyph has no outline, then loca[n] = loca [n+1]. 
        //In the particular case of the last glyph(s), loca[n] will be equal the length of the glyph data ('glyf') table. 
        //The offsets must be in ascending order with loca[n] <= loca[n+1].

        //Most routines will look at the 'maxp' table to determine the number of glyphs in the font, but the value in the 'loca' table must agree.

        //There are two versions of this table, the short and the long. The version is specified in the indexToLocFormat entry in the 'head' table.

        uint[] _offsets;
        public GlyphLocations(int glyphCount, bool isLongVersion)
        {
            _offsets = new uint[glyphCount + 1];
            this.IsLongVersion = isLongVersion;
        }
        public bool IsLongVersion { get; private set; }
        public uint[] Offsets => _offsets;
        public int GlyphCount => _offsets.Length - 1;

        protected override void ReadContentFrom(BinaryReader reader)
        {
            //Short version
            //Type 	Name 	Description
            //USHORT 	offsets[n] 	The actual local offset divided by 2 is stored. 
            //The value of n is numGlyphs + 1. 
            //The value for numGlyphs is found in the 'maxp' table.
            //-------------------------
            //Long version
            //Type 	Name 	Description
            //ULONG 	offsets[n] 	The actual local offset is stored.
            //The value of n is numGlyphs + 1. The value for numGlyphs is found in the 'maxp' table.

            //Note that the local offsets should be long-aligned, i.e., multiples of 4. Offsets which are not long-aligned may seriously degrade performance of some processors. 

            int glyphCount = GlyphCount;
            int lim = glyphCount + 1;
            _offsets = new uint[lim];
            if (IsLongVersion)
            {
                //long version
                for (int i = 0; i < lim; i++)
                {
                    _offsets[i] = reader.ReadUInt32();
                }
            }
            else
            {
                //short version
                for (int i = 0; i < lim; i++)
                {
                    _offsets[i] = (uint)(reader.ReadUInt16() << 1); // =*2
                }
            }
        }
    }
}
