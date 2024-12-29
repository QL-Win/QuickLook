//MIT, 2020-present, WinterDev
using System;
using System.Collections.Generic;
using Typography.OpenFont.Tables;

namespace Typography.OpenFont
{
    /// <summary>
    /// supported langs, designed langs 
    /// </summary>
    public class Languages
    {
        /// <summary>
        /// meta table's supported langs
        /// </summary>
        public string[] SupportedLangs { get; private set; }
        /// <summary>
        /// meta table's design langs
        /// </summary>
        public string[] DesignLangs { get; private set; }

        public ushort OS2Version { get; private set; }//for translate unicode ranges
        public uint UnicodeRange1 { get; private set; }
        public uint UnicodeRange2 { get; private set; }
        public uint UnicodeRange3 { get; private set; }
        public uint UnicodeRange4 { get; private set; }

        /// <summary>
        /// actual GSUB script list
        /// </summary>
        public ScriptList GSUBScriptList { get; private set; }
        /// <summary>
        /// actual GPOS script list
        /// </summary>
        public ScriptList GPOSScriptList { get; private set; }

        Cmap _cmap;

        internal void Update(OS2Table os2Tabble, Meta meta, Cmap cmap, GSUB gsub, GPOS gpos)
        {
            //https://docs.microsoft.com/en-us/typography/opentype/spec/os2#ur


            //This field is used to specify the Unicode blocks or ranges encompassed by the font file in 'cmap' subtables for platform 3,
            //encoding ID 1 (Microsoft platform, Unicode BMP) and platform 3,
            //encoding ID 10 (Microsoft platform, Unicode full repertoire). 
            //If a bit is set (1), then the Unicode ranges assigned to that bit are considered functional.
            //If the bit is clear (0), then the range is not considered functional. 

            //unicode BMP (Basic Multilingual Plane),OR plane0 (see https://unicode.org/roadmaps/bmp/)

            //Each of the bits is treated as an independent flag and the bits can be set in any combination.
            //The determination of “functional” is left up to the font designer,
            //although character set selection should attempt to be functional by ranges if at all possible. 

            //--------------
            //Different versions of the OS/2 table were created when different Unicode versions were current,
            //and the initial specification for a given version defined fewer bit assignments than for later versions. 
            //Some applications may not support all assignments for fonts that have earlier OS/2 versions.

            //All of the bit assignments listed above are valid for any version of the OS/2 table,
            //though OS/2 versions 1 and 2 were specified with some assignments that did not correspond to well-defined Unicode ranges and 
            //that conflict with later assignments — see the details below.
            //If a font has a version 1 or version 2 OS/2 table with one of these bits set, 
            //the obsolete assignment may be the intended interpretation.
            //Because these assignments do not correspond to well-defined ranges, 
            //however, the implied character coverage is unclear.

            //Version 0: When version 0 was first specified, no bit assignments were defined.
            //Some applications may ignore these fields in a version 0 OS/2 table.

            //Version 1:
            //Version 1 was first specified concurrent with Unicode 1.1,
            //and bit assigments were defined for bits 0 to 69 only. With fonts that have a version 1 table, 
            //some applications might recognize only bits 0 to 69.

            //Also, version 1 was specified with some bit assignments that did not correspond to a well-defined Unicode range:

            //    Bit 8: “Greek Symbols and Coptic” (bit 7 was specified as “Basic Greek”)
            //    Bit 12: “Hebrew Extended” (bit 11 was specified as “Basic Hebrew”)
            //    Bit 14: “Arabic Extended” (bit 13 was specified as “Basic Arabic”)
            //    Bit 27: “Georgian Extended” (bit 26 was specified as “Basic Georgian”)

            //These assignments were discontinued as of version 2.

            //In addition, versions 1 and 2 were defined with bit 53 specified as “CJK Miscellaneous”,
            //which also does not correspond to any well-defined Unicode range. 
            //This assignment was discontinued as of version 3.

            //Version 2:
            //Version 2 was defined in OpenType 1.1, which was concurrent with Unicode 2.1.
            //At that time, bit assignments were defined for bits 0 to 69 only. 
            //Bit assignments for version 2 were updated in OpenType 1.3, 
            //adding assignments for bits 70 to 83 corresponding to new blocks assigned in Unicode 2.0 and Unicode 3.0.
            //With fonts that have a version 2 table, 
            //some applications might recognize only those bits assigned in OpenType 1.2 or OpenType 1.3.

            //Also, the specification for version 2 continued to use a problematic assignment for bit 53 — 
            //see details for version 1. This assignment was discontinued as of version 3.

            //Version 3: Version 3 was defined in OpenType 1.4 with assignments for bits 84 to 91 corresponding to additional 
            //ranges in Unicode 3.2. 
            //In addition, some already-assigned bits were extended to cover additional Unicode ranges for related characters; s
            //ee details in the table above.

            //Version 4: Version 4 was defined in OpenType 1.5 with assignments for bit 58 and bits 92 to 122 corresponding to additional ranges in Unicode 5.1. 
            //Also, bits 8, 12, 14, 27 and 53 were re-assigned (see version 1 for previous assignments). 
            //In addition, some already-assigned bits were extended to cover additional Unicode ranges for related characters; 
            //see details in the table above.

            OS2Version = os2Tabble.version;
            UnicodeRange1 = os2Tabble.ulUnicodeRange1;
            UnicodeRange2 = os2Tabble.ulUnicodeRange2;
            UnicodeRange3 = os2Tabble.ulUnicodeRange3;
            UnicodeRange4 = os2Tabble.ulUnicodeRange4;
            //ULONG 	ulUnicodeRange1 	Bits 0-31
            //ULONG 	ulUnicodeRange2 	Bits 32-63
            //ULONG 	ulUnicodeRange3 	Bits 64-95
            //ULONG 	ulUnicodeRange4 	Bits 96-127



            //-------
            //IMPORTANT:***
            //All available bits were exhausted as of Unicode 5.1.  ***
            //The bit assignments were last updated for OS/2 version 4 in OpenType 1.5. 
            //There are many additional ranges supported in the current version of Unicode that are not supported by these fields in the OS/2 table. 
            //
            //See the 'dlng' and 'slng' tags in the 'meta' table for an alternate mechanism to declare 
            //what scripts or languages that a font can support or is designed for. 
            //-------


            if (meta != null)
            {
                SupportedLangs = meta.SupportedLanguageTags;
                DesignLangs = meta.DesignLanguageTags;
            }

            //----
            //gsub and gpos contains actual script_list that are in the typeface
            GSUBScriptList = gsub?.ScriptList;
            GPOSScriptList = gpos?.ScriptList;

            _cmap = cmap;
        }

        /// <summary>
        /// contains glyph for the given unicode code point or not
        /// </summary>
        /// <param name="codepoint"></param>
        /// <returns></returns>
        public bool ContainGlyphForUnicode(int codepoint) => (_cmap != null) ? (_cmap.GetGlyphIndex(codepoint, 0, out bool skipNextCodePoint) > 0) : false;
    }


}