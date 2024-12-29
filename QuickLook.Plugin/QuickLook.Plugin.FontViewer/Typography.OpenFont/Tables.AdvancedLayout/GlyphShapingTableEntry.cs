//Apache2, 2016-present, WinterDev, Sam Hocevar <sam@hocevar.net>

using System.IO;

namespace Typography.OpenFont.Tables
{
    //from https://docs.microsoft.com/en-us/typography/opentype/spec/otff#otttables
    //Data Types

    // The following data types are used in the OpenType font file.All OpenType fonts use Motorola-style byte ordering (Big Endian):
    // Data     Type    Description
    // uint8    8-bit   unsigned integer.
    // int8     8-bit   signed integer.
    // uint16   16-bit  unsigned integer.
    // int16    16-bit  signed integer.
    // uint24   24-bit  unsigned integer.
    // uint32   32-bit  unsigned integer.
    // int32    32-bit  signed integer.
    // Fixed    32-bit  signed fixed-point number(16.16)
    // FWORD    int16   that describes a quantity in font design units.
    // UFWORD   uint16  that describes a quantity in font design units.
    // F2DOT14  16 - bit signed fixed number with the low 14 bits of fraction(2.14).
    // LONGDATETIME     Date represented in number of seconds since 12:00 midnight, January 1, 1904.The value is represented as a signed 64 - bit integer.
    // Tag Array of four uint8s(length = 32 bits) used to identify a script, language system, feature, or baseline
    // Offset16   Short offset to a table, same as uint16, NULL offset = 0x0000
    // Offset32   Long offset to a table, same as uint32, NULL offset = 0x00000000

    // https://docs.microsoft.com/en-us/typography/opentype/spec/gpos
    // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub

    public abstract class GlyphShapingTableEntry : TableEntry
    {

        public ushort MajorVersion { get; private set; }
        public ushort MinorVersion { get; private set; }

        public ScriptList ScriptList { get; private set; }
        public FeatureList FeatureList { get; private set; }

        /// <summary>
        /// read script_list, feature_list, and skip look up table
        /// </summary>
        internal bool OnlyScriptList { get; set; } //

        protected override void ReadContentFrom(BinaryReader reader)
        {
            //-------------------------------------------
            // GPOS/GSUB Header
            // The GPOS/GSUB table begins with a header that contains a version number for the table. Two versions are defined.
            // Version 1.0 contains offsets to three tables: ScriptList, FeatureList, and LookupList.
            // Version 1.1 also includes an offset to a FeatureVariations table.
            // For descriptions of these tables, see the chapter, OpenType Layout Common Table Formats .
            // Example 1 at the end of this chapter shows a GPOS/GSUB Header table definition.
            //
            // GPOS/GSUB Header, Version 1.0
            // Value     Type               Description
            // uint16    MajorVersion       Major version of the GPOS/GSUB table, = 1
            // uint16    MinorVersion       Minor version of the GPOS/GSUB table, = 0
            // Offset16  ScriptList         Offset to ScriptList table, from beginning of GPOS/GSUB table
            // Offset16  FeatureList        Offset to FeatureList table, from beginning of GPOS/GSUB table
            // Offset16  LookupList         Offset to LookupList table, from beginning of GPOS/GSUB table
            //
            // GPOS/GSUB Header, Version 1.1
            // Value     Type               Description
            // uint16    MajorVersion       Major version of the GPOS/GSUB table, = 1
            // uint16    MinorVersion       Minor version of the GPOS/GSUB table, = 1
            // Offset16  ScriptList         Offset to ScriptList table, from beginning of GPOS/GSUB table
            // Offset16  FeatureList        Offset to FeatureList table, from beginning of GPOS/GSUB table
            // Offset16  LookupList         Offset to LookupList table, from beginning of GPOS/GSUB table
            // Offset32  FeatureVariations  Offset to FeatureVariations table, from beginning of GPOS/GSUB table (may be NULL)

            long tableStartAt = reader.BaseStream.Position;

            MajorVersion = reader.ReadUInt16();
            MinorVersion = reader.ReadUInt16();

            ushort scriptListOffset = reader.ReadUInt16(); // from beginning of table
            ushort featureListOffset = reader.ReadUInt16(); // from beginning of table
            ushort lookupListOffset = reader.ReadUInt16(); // from beginning of table
            uint featureVariations = (MinorVersion == 1) ? reader.ReadUInt32() : 0; // from beginning of table

            //-----------------------
            //1. scriptlist
            ScriptList = ScriptList.CreateFrom(reader, tableStartAt + scriptListOffset);

            if (OnlyScriptList) return; //for preview script-list and feature list only

            //-----------------------
            //2. feature list

            FeatureList = FeatureList.CreateFrom(reader, tableStartAt + featureListOffset);

            //3. lookup list
            ReadLookupListTable(reader, tableStartAt + lookupListOffset);

            //-----------------------
            //4. feature variations
            if (featureVariations > 0)
            {
                ReadFeatureVariations(reader, tableStartAt + featureVariations);
            }
        }

        void ReadLookupListTable(BinaryReader reader, long lookupListBeginAt)
        {
            //https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2
            // -----------------------
            // LookupList table
            // -----------------------
            // Type      Name                 Description
            // uint16    LookupCount          Number of lookups in this table
            // Offset16  Lookup[LookupCount]  Array of offsets to Lookup tables-from beginning of LookupList -zero based (first lookup is Lookup index = 0)
            // -----------------------
            //
            // Lookup Table
            // A Lookup table (Lookup) defines the specific conditions, type,
            // and results of a substitution or positioning action that is used to implement a feature.
            // For example, a substitution operation requires a list of target glyph indices to be replaced,
            // a list of replacement glyph indices, and a description of the type of substitution action.
            // Each Lookup table may contain only one type of information (LookupType),
            // determined by whether the lookup is part of a GSUB or GPOS table. GSUB supports eight LookupTypes,
            // and GPOS supports nine LookupTypes (for details about LookupTypes, see the GSUB and GPOS chapters of the document).
            //
            // Each LookupType is defined with one or more subtables,
            // and each subtable definition provides a different representation format.
            // The format is determined by the content of the information required for an operation and by required storage efficiency.
            // When glyph information is best presented in more than one format,
            // a single lookup may contain more than one subtable, as long as all the subtables are the same LookupType.
            // For example, within a given lookup, a glyph index array format may best represent one set of target glyphs,
            // whereas a glyph index range format may be better for another set of target glyphs.
            //
            // During text processing, a client applies a lookup to each glyph in the string before moving to the next lookup.
            // A lookup is finished for a glyph after the client makes the substitution/positioning operation.
            // To move to the “next” glyph, the client will typically skip all the glyphs that participated in the lookup operation: glyphs
            // that were substituted/positioned as well as any other glyphs that formed a context for the operation.
            // However, in the case of pair positioning operations (i.e., kerning),
            // the “next” glyph in a sequence may be the second glyph of the positioned pair (see pair positioning lookup for details).
            //
            // A Lookup table contains a LookupType, specified as an integer, that defines the type of information stored in the lookup.
            // The LookupFlag specifies lookup qualifiers that assist a text-processing client in substituting or positioning glyphs.
            // The SubTableCount specifies the total number of SubTables.
            // The SubTable array specifies offsets, measured from the beginning of the Lookup table, to each SubTable enumerated in the SubTable array.
            //
            // Lookup table
            // --------------------------------
            // Type      Name                     Description
            // unit16    LookupType               Different enumerations for GSUB and GPOS
            // unit16    LookupFlag               Lookup qualifiers
            // unit16    SubTableCount            Number of SubTables for this lookup
            // Offset16  SubTable[SubTableCount]  Array of offsets to SubTables-from beginning of Lookup table
            // uint16    MarkFilteringSet         Index (base 0) into GDEF mark glyph sets structure.
            //                                    *** This field is only present if bit UseMarkFilteringSet of lookup flags is set.
            // --------------------------------


            // --------------------------------
            //The LookupFlag uses two bytes of data: 

            //Each of the first four bits can be set in order to specify additional instructions for applying a lookup to a glyph string.The LookUpFlag bit enumeration table provides details about the use of these bits.
            //The fifth bit indicates the presence of a MarkFilteringSet field in the Lookup table. 
            //The next three bits are reserved for future use.

            //The high byte is set to specify the type of mark attachment.


            //LookupFlag bit enumeration
            //Type    Name                    Description
            //0x0001  rightToLeft             This bit relates only to the correct processing of the cursive attachment lookup type(GPOS lookup type 3).When this bit is set, the last glyph in a given sequence to which the cursive attachment lookup is applied, will be positioned on the baseline.
            //                                Note: Setting of this bit is not intended to be used by operating systems or applications to determine text direction.
            //0x0002  ignoreBaseGlyphs        If set, skips over base glyphs
            //0x0004  ignoreLigatures         If set, skips over ligatures
            //0x0008  ignoreMarks             If set, skips over all combining marks
            //0x0010  useMarkFilteringSet     If set, indicates that the lookup table structure is followed by a MarkFilteringSet field.
            //                                The layout engine skips over all mark glyphs not in the mark filtering set indicated.
            //0x00E0  reserved                For future use(Set to zero)
            //0xFF00  markAttachmentType      If not zero, skips over all marks of attachment type different from specified.
            // --------------------------------


            reader.BaseStream.Seek(lookupListBeginAt, SeekOrigin.Begin);
            ushort lookupCount = reader.ReadUInt16();
            ushort[] lookupTableOffsets = Utils.ReadUInt16Array(reader, lookupCount);

            //----------------------------------------------
            //load each sub table
            
            foreach (ushort lookupTableOffset in lookupTableOffsets)
            {
                long lookupTablePos = lookupListBeginAt + lookupTableOffset;
                reader.BaseStream.Seek(lookupTablePos, SeekOrigin.Begin);

                ushort lookupType = reader.ReadUInt16(); //Each Lookup table may contain only one type of information (LookupType)
                ushort lookupFlags = reader.ReadUInt16();
                ushort subTableCount = reader.ReadUInt16();

                //Each LookupType is defined with one or more subtables, and each subtable definition provides a different representation format
                ushort[] subTableOffsets = Utils.ReadUInt16Array(reader, subTableCount);

                ushort markFilteringSet =
                    ((lookupFlags & 0x0010) == 0x0010) ? reader.ReadUInt16() : (ushort)0;

                ReadLookupTable(reader,
                        lookupTablePos,
                        lookupType,
                        lookupFlags,
                        subTableOffsets, //Array of offsets to SubTables-from beginning of Lookup table
                        markFilteringSet);
            }
        }

        protected abstract void ReadLookupTable(BinaryReader reader, long lookupTablePos,
                                                ushort lookupType, ushort lookupFlags,
                                                ushort[] subTableOffsets, ushort markFilteringSet);
        protected abstract void ReadFeatureVariations(BinaryReader reader, long featureVariationsBeginAt);


    }
}
