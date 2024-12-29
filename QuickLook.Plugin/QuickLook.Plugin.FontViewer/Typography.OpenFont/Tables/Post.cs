//Apache2, 2016-present, WinterDev

using System.IO;
using System.Collections.Generic;

namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/post

    //post — PostScript Table 
    //This table contains additional information needed to use TrueType or OpenType™ fonts on PostScript printers. 
    //This includes data for the FontInfo dictionary entry and the PostScript names of all the glyphs. 
    //For more information about PostScript names, see the Adobe document Unicode and Glyph Names.

    //Versions 1.0, 2.0, and 2.5 refer to TrueType fonts and OpenType fonts with TrueType data. 
    //OpenType fonts with TrueType data may also use Version 3.0. OpenType fonts with CFF data use Version 3.0 only.
    //Header

    //Fixed =>	32-bit signed fixed-point number (16.16)

    //The table begins as follows:
    //Type 	    Name 	            Description
    //Fixed 	Version 	        0x00010000 for version 1.0
    //                              0x00020000 for version 2.0
    //                              0x00025000 for version 2.5 (deprecated)
    //                              0x00030000 for version 3.0
    //Fixed 	italicAngle 	    Italic angle in counter-clockwise degrees from the vertical. Zero for upright text, negative for text that leans to the right (forward).
    //FWord 	underlinePosition 	This is the suggested distance of the top of the underline from the baseline (negative values indicate below baseline).   
    //                              The PostScript definition of this FontInfo dictionary key (the y coordinate of the center of the stroke) is not used for historical reasons.
    //                              The value of the PostScript key may be calculated by subtracting half the underlineThickness from the value of this field.
    //FWord 	underlineThickness 	Suggested values for the underline thickness.
    //uint32 	isFixedPitch 	    Set to 0 if the font is proportionally spaced, non-zero if the font is not proportionally spaced (i.e. monospaced).
    //uint32 	minMemType42 	    Minimum memory usage when an OpenType font is downloaded.
    //uint32 	maxMemType42 	    Maximum memory usage when an OpenType font is downloaded.
    //uint32 	minMemType1 	    Minimum memory usage when an OpenType font is downloaded as a Type 1 font.
    //uint32 	maxMemType1 	    Maximum memory usage when an OpenType font is downloaded as a Type 1 font.
    //---------

    //The last four entries in the table are present because PostScript drivers can do better memory management
    //if the virtual memory (VM) requirements of a downloadable OpenType font are known before the font is downloaded.
    //This information should be supplied if known.
    //If it is not known, set the value to zero. 
    //The driver will still work but will be less efficient.

    //Maximum memory usage is minimum memory usage plus maximum runtime memory use.
    //Maximum runtime memory use depends on the maximum band size of any bitmap potentially rasterized by the font scaler. 
    //Runtime memory usage could be calculated by rendering characters at different point sizes and comparing memory use.

    class PostTable : TableEntry
    {
        public const string _N = "post";
        public override string Name => _N;
        // 
        Dictionary<ushort, string> _glyphNames;
        Dictionary<string, ushort> _glyphIndiceByName;

        public int Version { get; private set; }
        public uint ItalicAngle { get; private set; }
        public short UnderlinePosition { get; private set; }
        public short UnderlineThickness { get; private set; }

        protected override void ReadContentFrom(BinaryReader reader)
        {
            //header
            uint version = reader.ReadUInt32(); //16.16
            ItalicAngle = reader.ReadUInt32();
            UnderlinePosition = reader.ReadInt16();
            UnderlineThickness = reader.ReadInt16();
            uint isFixedPitch = reader.ReadUInt32();
            uint minMemType42 = reader.ReadUInt32();
            uint maxMemType42 = reader.ReadUInt32();
            uint minMemType1 = reader.ReadUInt32();
            uint maxMemType1 = reader.ReadUInt32();

            //If the version is 1.0 or 3.0, the table ends here. 

            //The additional entries for versions 2.0 and 2.5 are shown below.
            //Apple has defined a version 4.0 for use with Apple Advanced Typography (AAT), which is described in their documentation.

            // float version_f = (float)(version) / (1 << 16);

            switch (version)
            {
                case 0x00010000: //version 1
                    Version = 1;
                    break;
                case 0x00030000: //version3
                    Version = 3;
                    break;
                case 0x00020000: //version 2
                    {
                        Version = 2;

                        //Version 2.0

                        //This is the version required in order to supply PostScript glyph names for fonts which do not supply them elsewhere.
                        //A version 2.0 'post' table can be used in fonts with TrueType or CFF version 2 outlines.
                        //Type 	    Name 	                        Description
                        //uint16 	numberOfGlyphs 	                Number of glyphs (this should be the same as numGlyphs in 'maxp' table).
                        //uint16 	glyphNameIndex[numGlyphs]. 	    This is not an offset, but is the ordinal number of the glyph in 'post' string tables.
                        //int8 	    names[numberNewGlyphs] 	        Glyph names with length bytes [variable] (a Pascal string).

                        //This font file contains glyphs not in the standard Macintosh set,
                        //or the ordering of the glyphs in the font file differs from the standard Macintosh set. 
                        //The glyph name array maps the glyphs in this font to name index.
                        //....
                        //If you do not want to associate a PostScript name with a particular glyph, use index number 0 which points to the name .notdef.

                        _glyphNames = new Dictionary<ushort, string>();
                        ushort numOfGlyphs = reader.ReadUInt16();
                        ushort[] glyphNameIndice = Utils.ReadUInt16Array(reader, numOfGlyphs);//***  
                        string[] stdMacGlyphNames = MacPostFormat1.GetStdMacGlyphNames();

                        for (ushort i = 0; i < numOfGlyphs; ++i)
                        {
                            ushort glyphNameIndex = glyphNameIndice[i];
                            if (glyphNameIndex < 258)
                            {
                                //If the name index is between 0 and 257, treat the name index as a glyph index in the Macintosh standard order.  
                                //replace? 
                                _glyphNames[i] = stdMacGlyphNames[glyphNameIndex];
                            }
                            else
                            {
                                //If the name index is between 258 and 65535, 
                                //then subtract 258 and use that to index into the list of Pascal strings at the end of the table. 
                                //Thus a given font may map some of its glyphs to the standard glyph names, and some to its own names.

                                //258 and 65535, 
                                int len = reader.ReadByte(); //name len 
                                _glyphNames.Add(i, System.Text.Encoding.UTF8.GetString(reader.ReadBytes(len), 0, len));
                            }
                        }

                    }
                    break;
                default:
                    {
                        return;
                        throw new System.NotSupportedException();
                    }
                case 0x00025000:
                    //deprecated ??
                    throw new System.NotSupportedException();
            }

        }


        internal Dictionary<ushort, string> GlyphNames => _glyphNames;
        //
        internal ushort GetGlyphIndex(string glyphName)
        {
            if (_glyphNames == null)
            {
                return 0; //not found!
            }
            //
            if (_glyphIndiceByName == null)
            {
                //------
                //create a cache
                _glyphIndiceByName = new Dictionary<string, ushort>();
                foreach (var kp in _glyphNames)
                {
                    //TODO: review how to handle duplicated glyph name
                    //1. report the error
                    //2. handle ...

                    _glyphIndiceByName[kp.Value] = kp.Key;
                    //_glyphIndiceByName.Add(kp.Value, kp.Key);
                }
            }
            _glyphIndiceByName.TryGetValue(glyphName, out ushort found);
            return found;
        }

    }




    //Version 2.5 (deprecated)

    //This version of the 'post' table has been deprecated as of OpenType Specification v1.3.

    //This version provides a space-saving table for TrueType-based fonts which contain a pure subset of, or a simple reordering of, the standard Macintosh glyph set.
    //Type 	Name 	Description
    //USHORT 	numberOfGlyphs 	Number of glyphs
    //CHAR 	offset[numGlyphs] 	Difference between graphic index and standard order of glyph

    //This version is useful for TrueType-based font files that contain only glyphs in the standard Macintosh glyph set but which have those glyphs arranged in a non-standard order or which are missing some glyphs. The table contains one byte for each glyph in the font file. The byte is treated as a signed offset that maps the glyph index used in this font into the standard glyph index. In other words, assuming that the font contains the three glyphs A, B, and C which are the 37th, 38th, and 39th glyphs in the standard ordering, the 'post' table would contain the bytes +36, +36, +36. This format has been deprecated by Apple, as of February 2000.
    //Version 3.0

    //The version makes it possible to create a font that is not burdened with a large 'post' table set of glyph names. A version 3.0 'post' table can be used by OpenType fonts with TrueType or CFF (version 1 or 2) data.

    //This version specifies that no PostScript name information is provided for the glyphs in this font file. The printing behavior of this version on PostScript printers is unspecified, except that it should not result in a fatal or unrecoverable error. Some drivers may print nothing; other drivers may attempt to print using a default naming scheme.

    //Windows makes use of the italic angle value in the 'post' table but does not actually require any glyph names to be stored as Pascal strings.
    //'post' Table and OpenType Font Variations

    //In a variable font, various font-metric values within the 'post' table may need to be adjusted for different variation instances. Variation data for 'post' entries can be provided in the metrics variations ('MVAR') table. Different 'post' entries are associated with particular variation data in the 'MVAR' table using value tags, as follows:
    //'post' entry 	Tag
    //underlinePosition 	'undo'
    //underlineThickness 	'unds'

    //    Note: The italicAngle value is not adjusted by variation data since this corresponds to the 'slnt' variation axis that can be used to define a font’s variation space. Appropriate post.italicAngle values for a variation instance can be derived from the 'slnt' user coordinates that are used to select a particular variation instance. See the discussion of the 'slnt' axis in the Variation Axis Tags section of the 'fvar' table chapter for details on the relationship between italicAngle and the 'slnt' axis.

    //For general information on OpenType Font Variations, see the chapter, OpenType Font Variations Overview.




}