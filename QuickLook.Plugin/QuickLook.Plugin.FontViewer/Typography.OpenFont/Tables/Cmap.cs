//Apache2, 2017-present, WinterDev
//Apache2, 2014-2016, Samuel Carlsson, WinterDev

using System;
using System.Collections.Generic;
using System.IO;
namespace Typography.OpenFont.Tables
{

    //---------------------------------------------------
    //cmap - Character To Glyph Index Mapping Table
    //---------------------------------------------------
    //This table defines the mapping of character codes to the glyph index values used in the font. 
    //It may contain more than one subtable, in order to support more than one character encoding scheme.
    //Character codes that do not correspond to any glyph in the font should be mapped to glyph index 0.
    //The glyph at this location must be a special glyph representing a missing character, commonly known as .notdef.

    //The table header indicates the character encodings for which subtables are present.
    //Each subtable is in one of seven possible formats and begins with a format code indicating the format used.

    //The platform ID and platform - specific encoding ID in the header entry(and, in the case of the Macintosh platform,
    //the language field in the subtable itself) are used to specify a particular 'cmap' encoding.
    //The header entries must be sorted first by platform ID, then by platform - specific encoding ID,
    //and then by the language field in the corresponding subtable.Each platform ID, 
    //platform - specific encoding ID, and subtable language combination may appear only once in the 'cmap' table.

    //When building a Unicode font for Windows, the platform ID should be 3 and the encoding ID should be 1.
    //When building a symbol font for Windows, the platform ID should be 3 and the encoding ID should be 0.
    //When building a font that will be used on the Macintosh, the platform ID should be 1 and the encoding ID should be 0.

    //All Microsoft Unicode BMP encodings(Platform ID = 3, Encoding ID = 1) must provide at least a Format 4 'cmap' subtable.
    //If the font is meant to support supplementary(non - BMP) Unicode characters,
    //it will additionally need a Format 12 subtable with a platform encoding ID 10.
    //The contents of the Format 12 subtable need to be a superset of the contents of the Format 4 subtable.
    //Microsoft strongly recommends using a BMP Unicode 'cmap' for all fonts. However, some other encodings that appear in current fonts follow:

    //Windows Encodings
    //Platform ID Encoding ID Description
    //3   0   Symbol
    //3   1   Unicode BMP(UCS - 2)
    //3   2   ShiftJIS
    //3   3   PRC
    //3   4   Big5
    //3   5   Wansung
    //3   6   Johab
    //3   7   Reserved
    //3   8   Reserved
    //3   9   Reserved
    //3   10  Unicode UCS - 4
    //---------------------------------------------------


    ////////////////////////////////////////////////////////////////////////
    //from https://docs.microsoft.com/en-us/typography/opentype/processing-part2
    //CMAP Table
    //Every glyph in a TrueType font is identified by a unique Glyph ID (GID),
    //a simple sequential numbering of all the glyphs in the font. 
    //These GIDs are mapped to character codepoints in the font's CMAP table.
    //In OpenType fonts, the principal mapping is to Unicode codepoints; that is, 
    //the GIDs of nominal glyph representations of specific characters are mapped to appropriate Unicode values.

    //The key to OpenType glyph processing is that not every glyph in a font is directly mapped to a codepoint. 
    //Variant glyph forms, ligatures, dynamically composed diacritics and other rendering forms do not require entries in the CMAP table. 
    //Rather, their GIDs are mapped in layout features to the GIDs of nominal character forms, 
    //i.e. to those glyphs that do have CMAP entries. This is the heart of glyph processing: the mapping of GIDs to each other, 
    //rather than directly to character codepoints.

    //In order for fonts to be able to correctly render text, 
    //font developers must ensure that the correct nominal glyph form GIDs are mapped to the correct Unicode codepoints. 
    //Application developers, of course, must ensure that their applications correctly manage input and storage of Unicode text codepoints,
    //or map correctly to these codepoints from other codepages and character sets. 
    ////////////////////////////////////////////////////////////////////////

    public class Cmap : TableEntry
    {
        //https://docs.microsoft.com/en-us/typography/opentype/spec/cmap

        public const string _N = "cmap";
        public override string Name => _N;

        CharacterMap[] _charMaps = null;
        List<CharMapFormat14> _charMap14List;
        Dictionary<int, ushort> _codepointToGlyphs = new Dictionary<int, ushort>();


        /// <summary>
        /// find glyph index from given codepoint(s)
        /// </summary>
        /// <param name="codepoint"></param>
        /// <param name="nextCodepoint"></param>
        /// <returns>glyph index</returns>

        public ushort GetGlyphIndex(int codepoint, int nextCodepoint, out bool skipNextCodepoint)
        {
            // https://docs.microsoft.com/en-us/typography/opentype/spec/cmap
            // "character codes that do not correspond to any glyph in the font should be mapped to glyph index 0."

            skipNextCodepoint = false; //default

            if (!_codepointToGlyphs.TryGetValue(codepoint, out ushort found))
            {
                for (int i = 0; i < _charMaps.Length; ++i)
                {
                    CharacterMap cmap = _charMaps[i];



                    if (found == 0)
                    {
                        found = cmap.GetGlyphIndex(codepoint);
                    }
                    else if (cmap.PlatformId == 3 && cmap.EncodingId == 1)
                    {
                        //...When building a Unicode font for Windows, 
                        // the platform ID should be 3 and the encoding ID should be 1
                        ushort glyphIndex = cmap.GetGlyphIndex(codepoint); //glyphIndex=> gid
                        if (glyphIndex != 0)
                        {
                            found = glyphIndex;
                        }
                    }
                }
                _codepointToGlyphs[codepoint] = found;
            }

            // If there is a second codepoint, we are asked whether this is an UVS sequence
            //  -> if true, return a glyph ID
            //  -> otherwise, return 0
            if (nextCodepoint > 0 && _charMap14List != null)
            {
                foreach (CharMapFormat14 cmap14 in _charMap14List)
                {
                    ushort glyphIndex = cmap14.CharacterPairToGlyphIndex(codepoint, found, nextCodepoint);
                    if (glyphIndex > 0)
                    {
                        skipNextCodepoint = true;
                        return glyphIndex;
                    }
                }
            }
            return found;
        }

        protected override void ReadContentFrom(BinaryReader input)
        {
            //https://www.microsoft.com/typography/otspec/cmap.htm
            long beginAt = input.BaseStream.Position;
            //
            ushort version = input.ReadUInt16(); // 0
            ushort tableCount = input.ReadUInt16();

            ushort[] platformIds = new ushort[tableCount];
            ushort[] encodingIds = new ushort[tableCount];
            uint[] offsets = new uint[tableCount];
            for (int i = 0; i < tableCount; i++)
            {
                platformIds[i] = input.ReadUInt16();
                encodingIds[i] = input.ReadUInt16();
                offsets[i] = input.ReadUInt32();
            }

            _charMaps = new CharacterMap[tableCount];
            for (int i = 0; i < tableCount; i++)
            {
                input.BaseStream.Seek(beginAt + offsets[i], SeekOrigin.Begin);
                CharacterMap cmap = ReadCharacterMap(input);
                cmap.PlatformId = platformIds[i];
                cmap.EncodingId = encodingIds[i];
                _charMaps[i] = cmap;

                //
                if (cmap is CharMapFormat14 cmap14)
                {
                    if (_charMap14List == null) _charMap14List = new List<CharMapFormat14>();
                    //
                    _charMap14List.Add(cmap14);
                }
            }
        }

        static CharacterMap ReadFormat_0(BinaryReader input)
        {
            ushort length = input.ReadUInt16();
            //Format 0: Byte encoding table
            //This is the Apple standard character to glyph index mapping table.
            //Type  	Name 	        Description
            //uint16 	format 	        Format number is set to 0.
            //uint16 	length 	        This is the length in bytes of the subtable.
            //uint16 	language 	    Please see “Note on the language field in 'cmap' subtables“ in this document.
            //uint8 	glyphIdArray[256] 	An array that maps character codes to glyph index values.
            //-----------
            //This is a simple 1 to 1 mapping of character codes to glyph indices. 
            //The glyph set is limited to 256. Note that if this format is used to index into a larger glyph set,
            //only the first 256 glyphs will be accessible. 

            ushort language = input.ReadUInt16();
            byte[] only256Glyphs = input.ReadBytes(256);
            ushort[] only256UInt16Glyphs = new ushort[256];
            for (int i = 255; i >= 0; --i)
            {
                //expand
                only256UInt16Glyphs[i] = only256Glyphs[i];
            }
            //convert to format4 cmap table
            ushort[] startArray = new ushort[] { 0, 0xFFFF };
            ushort[] endArray = new ushort[] { 255, 0xFFFF };
            ushort[] deltaArray = new ushort[] { 0, 1 };
            ushort[] offsetArray = new ushort[] { 4, 0 };
            return new CharMapFormat4(startArray, endArray, deltaArray, offsetArray, only256UInt16Glyphs);
        }

        static CharacterMap ReadFormat_2(BinaryReader input)
        {
            //Format 2: High - byte mapping through table

            //This subtable is useful for the national character code standards used for Japanese, Chinese, and Korean characters.
            //These code standards use a mixed 8 / 16 - bit encoding, 
            //in which certain byte values signal the first byte of a 2 - byte character(but these values are also legal as the second byte of a 2 - byte character).
            //
            //In addition, even for the 2 - byte characters, the mapping of character codes to glyph index values depends heavily on the first byte.
            //Consequently, the table begins with an array that maps the first byte to a SubHeader record.
            //For 2 - byte character codes, the SubHeader is used to map the second byte's value through a subArray, as described below.
            //When processing mixed 8/16-bit text, SubHeader 0 is special: it is used for single-byte character codes. 
            //When SubHeader 0 is used, a second byte is not needed; the single byte value is mapped through the subArray.
            //-------------
            //  'cmap' Subtable Format 2:
            //-------------
            //  Type        Name        Description
            //  uint16      format      Format number is set to 2.
            //  uint16      length      This is the length in bytes of the subtable.
            //  uint16      language    Please see “Note on the language field in 'cmap' subtables“ in this document.
            //  uint16      subHeaderKeys[256]  Array that maps high bytes to subHeaders: value is subHeader index * 8.
            //  SubHeader   subHeaders[]   Variable - length array of SubHeader records.
            //  uint16  glyphIndexArray[]  Variable - length array containing subarrays used for mapping the low byte of 2 - byte characters.
            //------------------
            //  A SubHeader is structured as follows:
            //  SubHeader Record:
            //  Type    Name            Description
            //  uint16  firstCode       First valid low byte for this SubHeader.
            //  uint16  entryCount      Number of valid low bytes for this SubHeader.
            //  int16   idDelta See     text below.
            //  uint16  idRangeOffset   See text below.
            //
            //  The firstCode and entryCount values specify a subrange that begins at firstCode and has a length equal to the value of entryCount.
            //This subrange stays within the 0 - 255 range of the byte being mapped.
            //Bytes outside of this subrange are mapped to glyph index 0(missing glyph).
            //The offset of the byte within this subrange is then used as index into a corresponding subarray of glyphIndexArray.
            //This subarray is also of length entryCount.
            //The value of the idRangeOffset is the number of bytes past the actual location of the idRangeOffset word
            //where the glyphIndexArray element corresponding to firstCode appears.
            //  Finally, if the value obtained from the subarray is not 0(which indicates the missing glyph),
            //you should add idDelta to it in order to get the glyphIndex.
            //The value idDelta permits the same subarray to be used for several different subheaders.
            //The idDelta arithmetic is modulo 65536.

            Utils.WarnUnimplemented("cmap subtable format 2");
            return new NullCharMap();
        }

        static CharMapFormat4 ReadFormat_4(BinaryReader input)
        {
            ushort lenOfSubTable = input.ReadUInt16(); //This is the length in bytes of the subtable. ****
            //This is the Microsoft standard character to glyph index mapping table for fonts that support Unicode ranges other than the range [U+D800 - U+DFFF] (defined as Surrogates Area, in Unicode v 3.0) 
            //which is used for UCS-4 characters.
            //If a font supports this character range (i.e. in turn supports the UCS-4 characters) a subtable in this format with a platform specific encoding ID 1 is yet needed,
            //in addition to a subtable in format 12 with a platform specific encoding ID 10. Please see details on format 12 below, for fonts that support UCS-4 characters on Windows.
            //  
            //This format is used when the character codes for the characters represented by a font fall into several contiguous ranges, 
            //possibly with holes in some or all of the ranges (that is, some of the codes in a range may not have a representation in the font). 
            //The format-dependent data is divided into three parts, which must occur in the following order:
            //    A four-word header gives parameters for an optimized search of the segment list;
            //    Four parallel arrays describe the segments (one segment for each contiguous range of codes);
            //    A variable-length array of glyph IDs (unsigned words).
            long tableStartEndAt = input.BaseStream.Position + lenOfSubTable;

            ushort language = input.ReadUInt16();
            //Note on the language field in 'cmap' subtables: 
            //The language field must be set to zero for all cmap subtables whose platform IDs are other than Macintosh (platform ID 1).
            //For cmap subtables whose platform IDs are Macintosh, set this field to the Macintosh language ID of the cmap subtable plus one, 
            //or to zero if the cmap subtable is not language-specific.
            //For example, a Mac OS Turkish cmap subtable must set this field to 18, since the Macintosh language ID for Turkish is 17. 
            //A Mac OS Roman cmap subtable must set this field to 0, since Mac OS Roman is not a language-specific encoding.

            ushort segCountX2 = input.ReadUInt16(); //2 * segCount
            ushort searchRange = input.ReadUInt16(); //2 * (2**FLOOR(log2(segCount)))
            ushort entrySelector = input.ReadUInt16();//2 * (2**FLOOR(log2(segCount)))
            ushort rangeShift = input.ReadUInt16(); //2 * (2**FLOOR(log2(segCount)))
            int segCount = segCountX2 / 2;
            ushort[] endCode = Utils.ReadUInt16Array(input, segCount);//Ending character code for each segment, last = 0xFFFF.            
                                                                      //>To ensure that the search will terminate, the final endCode value must be 0xFFFF.
                                                                      //>This segment need not contain any valid mappings. It can simply map the single character code 0xFFFF to the missing character glyph, glyph 0.

            ushort Reserved = input.ReadUInt16(); // always 0
            ushort[] startCode = Utils.ReadUInt16Array(input, segCount); //Starting character code for each segment
            ushort[] idDelta = Utils.ReadUInt16Array(input, segCount); //Delta for all character codes in segment
            ushort[] idRangeOffset = Utils.ReadUInt16Array(input, segCount); //Offset in bytes to glyph indexArray, or 0   
                                                                             //------------------------------------------------------------------------------------ 
            long remainingLen = tableStartEndAt - input.BaseStream.Position;
            int recordNum2 = (int)(remainingLen / 2);
            ushort[] glyphIdArray = Utils.ReadUInt16Array(input, recordNum2);//Glyph index array                          
            return new CharMapFormat4(startCode, endCode, idDelta, idRangeOffset, glyphIdArray);
        }

        static CharMapFormat6 ReadFormat_6(BinaryReader input)
        {
            //Format 6: Trimmed table mapping
            //Type      Name        Description
            //uint16    format      Format number is set to 6.
            //uint16    length      This is the length in bytes of the subtable.
            //uint16    language    Please see “Note on the language field in 'cmap' subtables“ in this document.
            //uint16    firstCode   First character code of subrange.
            //uint16    entryCount  Number of character codes in subrange.
            //uint16    glyphIdArray[entryCount]   Array of glyph index values for character codes in the range.

            //The firstCode and entryCount values specify a subrange(beginning at firstCode, length = entryCount) within the range of possible character codes.
            //Codes outside of this subrange are mapped to glyph index 0.
            //The offset of the code(from the first code) within this subrange is used as index to the glyphIdArray, 
            //which provides the glyph index value.

            ushort length = input.ReadUInt16();
            ushort language = input.ReadUInt16();
            ushort firstCode = input.ReadUInt16();
            ushort entryCount = input.ReadUInt16();
            ushort[] glyphIdArray = Utils.ReadUInt16Array(input, entryCount);
            return new CharMapFormat6(firstCode, glyphIdArray);
        }

        static CharacterMap ReadFormat_12(BinaryReader input)
        {
            //TODO: test this again
            // Format 12: Segmented coverage
            //This is the Microsoft standard character to glyph index mapping table for fonts supporting the UCS - 4 characters 
            //in the Unicode Surrogates Area(U + D800 - U + DFFF).
            //It is a bit like format 4, in that it defines segments for sparse representation in 4 - byte character space.
            //Here's the subtable format:
            //'cmap' Subtable Format 12:
            //Type     Name      Description
            //uint16   format    Subtable format; set to 12.
            //uint16   reserved  Reserved; set to 0
            //uint32   length    Byte length of this subtable(including the header)
            //uint32   language  Please see “Note on the language field in 'cmap' subtables“ in this document.
            //uint32   numGroups Number of groupings which follow
            //SequentialMapGroup  groups[numGroups]   Array of SequentialMapGroup records.
            //
            //The sequential map group record is the same format as is used for the format 8 subtable.
            //The qualifications regarding 16 - bit character codes does not apply here, 
            //however, since characters codes are uniformly 32 - bit.
            //SequentialMapGroup Record:
            //Type    Name    Description
            //uint32  startCharCode   First character code in this group
            //uint32  endCharCode Last character code in this group
            //uint32  startGlyphID    Glyph index corresponding to the starting character code
            //
            //Groups must be sorted by increasing startCharCode.A group's endCharCode must be less than the startCharCode of the following group, 
            //if any. The endCharCode is used, rather than a count, because comparisons for group matching are usually done on an existing character code, 
            //and having the endCharCode be there explicitly saves the necessity of an addition per group.
            //
            //Fonts providing Unicode - encoded UCS - 4 character support for Windows 2000 and later, 
            //need to have a subtable with platform ID 3, platform specific encoding ID 1 in format 4;
            //and in addition, need to have a subtable for platform ID 3, platform specific encoding ID 10 in format 12.
            //Please note, that the content of format 12 subtable,
            //needs to be a super set of the content in the format 4 subtable.
            //The format 4 subtable needs to be in the cmap table to enable backward compatibility needs.

            ushort reserved = input.ReadUInt16();
#if DEBUG
            if (reserved != 0) { throw new OpenFontNotSupportedException(); }
#endif

            uint length = input.ReadUInt32();// Byte length of this subtable(including the header)
            uint language = input.ReadUInt32();
            uint numGroups = input.ReadUInt32();

#if DEBUG
            if (numGroups > int.MaxValue) { throw new OpenFontNotSupportedException(); }
#endif
            uint[] startCharCodes = new uint[(int)numGroups];
            uint[] endCharCodes = new uint[(int)numGroups];
            uint[] startGlyphIds = new uint[(int)numGroups];


            for (uint i = 0; i < numGroups; ++i)
            {
                //seq map group record
                startCharCodes[i] = input.ReadUInt32();
                endCharCodes[i] = input.ReadUInt32();
                startGlyphIds[i] = input.ReadUInt32();
            }
            return new CharMapFormat12(startCharCodes, endCharCodes, startGlyphIds);
        }

        private static CharacterMap ReadCharacterMap(BinaryReader input)
        {
            ushort format = input.ReadUInt16();
            switch (format)
            {
                default:
                    Utils.WarnUnimplemented("cmap subtable format {0}", format);
                    return new NullCharMap();
                case 0: return ReadFormat_0(input);
                case 2: return ReadFormat_2(input);
                case 4: return ReadFormat_4(input);
                case 6: return ReadFormat_6(input);
                case 12: return ReadFormat_12(input);
                case 14: return CharMapFormat14.Create(input);
            }
        }

        public void CollectUnicode(List<uint> unicodes)
        {
            for (int i = 0; i < _charMaps.Length; ++i)
            {
                _charMaps[i].CollectUnicodeChars(unicodes);
            }
        }
        public void CollectUnicode(int platform, List<uint> unicodes, List<ushort> glyphIndexList)
        {
            if (_charMaps.Length == 1)
            {

            }
            for (int i = 0; i < _charMaps.Length; ++i)
            {
                CharacterMap cmap = _charMaps[i];
                if (platform < 0)
                {
                    cmap.CollectUnicodeChars(unicodes, glyphIndexList);
                }
                else if (cmap.PlatformId == platform)
                {
                    cmap.CollectUnicodeChars(unicodes, glyphIndexList);
                }
            }
        }
    }
}
