//MIT, 2019-present, WinterDev

using System;
using System.Collections.Generic;
using System.IO;

namespace Typography.OpenFont.Tables
{
    /// <summary>
    /// The Justification table
    /// </summary>
    public partial class JSTF : TableEntry
    {
        //https://docs.microsoft.com/en-us/typography/opentype/spec/jstf

        public const string _N = "JSTF";
        public override string Name => _N;
        JstfScriptTable[] _jsftScriptTables;

        //The Justification table(JSTF) provides font developers with additional control over glyph substitution and
        //positioning in justified text.

        //Text-processing clients now have more options to expand or 
        //shrink word and glyph spacing so text fills the specified line length.

        protected override void ReadContentFrom(BinaryReader reader)
        {
            //test this with Arial font

            //JSTF header
            //Type              Name                                Description
            //uint16            majorVersion                        Major version of the JSTF table, = 1
            //uint16            minorVersion                        Minor version of the JSTF table, = 0
            //uint16            jstfScriptCount                     Number of JstfScriptRecords in this table
            //JstfScriptRecord  jstfScriptRecords[jstfScriptCount]  Array of JstfScriptRecords, in alphabetical order by jstfScriptTag

            //----------
            //JstfScriptRecord
            //Type 	            Name 	            Description
            //Tag 	            jstfScriptTag 	    4-byte JstfScript identification
            //Offset16 	        jstfScriptOffset 	Offset to JstfScript table, from beginning of JSTF Header

            long tableStartAt = reader.BaseStream.Position;
            //
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();
            ushort jstfScriptCount = reader.ReadUInt16();

            JstfScriptRecord[] recs = new JstfScriptRecord[jstfScriptCount];
            for (int i = 0; i < recs.Length; ++i)
            {
                recs[i] = new JstfScriptRecord(
                     Utils.TagToString(reader.ReadUInt32()),
                     reader.ReadUInt16()
                    );
            }

            _jsftScriptTables = new JstfScriptTable[recs.Length];
            for (int i = 0; i < recs.Length; ++i)
            {
                JstfScriptRecord rec = recs[i];
                reader.BaseStream.Position = tableStartAt + rec.jstfScriptOffset;

                JstfScriptTable jstfScriptTable = ReadJstfScriptTable(reader);
                jstfScriptTable.ScriptTag = rec.jstfScriptTag;
                _jsftScriptTables[i] = jstfScriptTable;
            }
        }
        readonly struct JstfScriptRecord
        {
            public readonly string jstfScriptTag;
            public readonly ushort jstfScriptOffset;
            public JstfScriptRecord(string jstfScriptTag, ushort jstfScriptOffset)
            {
                this.jstfScriptTag = jstfScriptTag;
                this.jstfScriptOffset = jstfScriptOffset;
            }
        }

        public class JstfScriptTable
        {
            public ushort[] extenderGlyphs;

            public JstfLangSysRecord defaultLangSys;
            public JstfLangSysRecord[] other;

            public JstfScriptTable()
            {
            }
            public string ScriptTag { get; set; }
#if DEBUG
            public override string ToString()
            {
                return ScriptTag;
            }
#endif
        }

        static JstfScriptTable ReadJstfScriptTable(BinaryReader reader)
        {
            //A Justification Script(JstfScript) table describes the justification information for a single script.
            //It consists of an offset to a table that defines extender glyphs(extenderGlyphOffset),
            //an offset to a default justification table for the script (defJstfLangSysOffset), 
            //and a count of the language systems that define justification data(jstfLangSysCount).

            //If a script uses the same justification information for all language systems, 
            //the font developer defines only the default JstfLangSys table and 
            //sets the jstfLangSysCount value to zero(0).

            //However, if any language system has unique justification suggestions,
            //jstfLangSysCount will be a positive value, 
            //and the JstfScript table must include an array of records(jstfLangSysRecords), 
            //one for each language system.Each JstfLangSysRecord contains a language system tag(jstfLangSysTag) and 
            //an offset to a justification language system table(jstfLangSysOffset).

            //In the jstfLangSysRecords array, records are ordered alphabetically by jstfLangSysTag.

            //JstfScript table
            //Type              Name                            Description
            //Offset16          extenderGlyphOffset             Offset to ExtenderGlyph table, from beginning of JstfScript table(may be NULL)
            //Offset16          defJstfLangSysOffset            Offset to default JstfLangSys table, from beginning of JstfScript table(may be NULL)
            //uint16            jstfLangSysCount                Number of JstfLangSysRecords in this table - may be zero(0)
            //JstfLangSysRecord jstfLangSysRecords[jstfLangSysCount]    Array of JstfLangSysRecords, in alphabetical order by JstfLangSysTag

            JstfScriptTable jstfScriptTable = new JstfScriptTable();

            long tableStartAt = reader.BaseStream.Position;

            ushort extenderGlyphOffset = reader.ReadUInt16();
            ushort defJstfLangSysOffset = reader.ReadUInt16();
            ushort jstfLangSysCount = reader.ReadUInt16();

            if (jstfLangSysCount > 0)
            {
                JstfLangSysRecord[] recs = new JstfLangSysRecord[jstfLangSysCount];
                for (int i = 0; i < jstfLangSysCount; ++i)
                {
                    recs[i] = ReadJstfLangSysRecord(reader);
                }
                jstfScriptTable.other = recs;
            }


            if (extenderGlyphOffset > 0)
            {
                reader.BaseStream.Position = tableStartAt + extenderGlyphOffset;
                jstfScriptTable.extenderGlyphs = ReadExtenderGlyphTable(reader);
            }

            if (defJstfLangSysOffset > 0)
            {
                reader.BaseStream.Position = tableStartAt + defJstfLangSysOffset;
                jstfScriptTable.defaultLangSys = ReadJstfLangSysRecord(reader);
            }
            return jstfScriptTable;
        }
        static ushort[] ReadExtenderGlyphTable(BinaryReader reader)
        {
            //Extender Glyph Table

            //The Extender Glyph table(ExtenderGlyph) lists indices of glyphs, 3
            //such as Arabic kashidas, 
            //that a client may insert to extend the length of the line for justification.
            //The table consists of a count of the extender glyphs for the script (glyphCount) and
            //an array of extender glyph indices(extenderGlyphs), arranged in increasing numerical order.

            //ExtenderGlyph table
            //Type    Name              Description
            //uint16  glyphCount        Number of extender glyphs in this script
            //uint16  extenderGlyphs[glyphCount]  Extender glyph IDs — in increasing numerical order

            ushort glyphCount = reader.ReadUInt16();
            return Utils.ReadUInt16Array(reader, glyphCount);
        }


        public struct JstfLangSysRecord
        {
            public JstfPriority[] jstfPriority;

        }
        static JstfLangSysRecord ReadJstfLangSysRecord(BinaryReader reader)
        {
            //Justification Language System Table

            //The Justification Language System(JstfLangSys) table contains an array of justification suggestions,
            //ordered by priority.
            //A text-processing client doing justification should begin with the suggestion that has a zero(0) priority,
            //and then-as necessary - apply suggestions of increasing priority until the text is justified.

            //The font developer defines the number and the meaning of the priority levels.
            //Each priority level stands alone; its suggestions are not added to the previous levels.
            //The JstfLangSys table consists of a count of the number of priority levels(jstfPriorityCount) and
            //an array of offsets to Justification Priority tables(jstfPriorityOffsets), 
            //stored in priority order. 

            //JstfLangSys table

            //stfLangSys table
            //Type       Name                                   Description
            //uint16     jstfPriorityCount                       Number of JstfPriority tables
            //Offset16   jstfPriorityOffsets[jstfPriorityCount]  Array of offsets to JstfPriority tables, from beginning of JstfLangSys table, in priority order

            long tableStartAt = reader.BaseStream.Position;
            ushort jstfPriorityCount = reader.ReadUInt16();
            ushort[] jstfPriorityOffsets = Utils.ReadUInt16Array(reader, jstfPriorityCount);

            JstfPriority[] jstPriorities = new JstfPriority[jstfPriorityCount];

            for (int i = 0; i < jstfPriorityOffsets.Length; ++i)
            {
                reader.BaseStream.Position = tableStartAt + jstfPriorityOffsets[i];
                jstPriorities[i] = ReadJstfPriority(reader);
            }

            return new JstfLangSysRecord() { jstfPriority = jstPriorities };

        }

        public class JstfPriority
        {
            //JstfPriority table

            //Type     Name                    Description
            //Offset16 shrinkageEnableGSUB     Offset to shrinkage-enable JstfGSUBModList table, from beginning of JstfPriority table(may be NULL)
            //Offset16 shrinkageDisableGSUB    Offset to shrinkage-disable JstfGSUBModList table, from beginning of JstfPriority table(may be NULL)
            public ushort shrinkageEnableGSUB;
            public ushort shrinkageDisableGSUB;

            //Offset16 shrinkageEnableGPOS     Offset to shrinkage-enable JstfGPOSModList table, from beginning of JstfPriority table(may be NULL)
            //Offset16 shrinkageDisableGPOS    Offset to shrinkage-disable JstfGPOSModList table, from beginning of JstfPriority table(may be NULL)

            public ushort shrinkageEnableGPOS;
            public ushort shrinkageDisableGPOS;

            //Offset16 shrinkageJstfMax        Offset to shrinkage JstfMax table, from beginning of JstfPriority table(may be NULL)
            public ushort shrinkageJstfMax;

            //Offset16 extensionEnableGSUB     Offset to extension-enable JstfGSUBModList table, from beginnning of JstfPriority table(may be NULL)
            //Offset16 extensionDisableGSUB    Offset to extension-disable JstfGSUBModList table, from beginning of JstfPriority table(may be NULL)

            public ushort extensionEnableGSUB;
            public ushort extensionDisableGSUB;

            //Offset16 extensionEnableGPOS     Offset to extension-enable JstfGPOSModList table, from beginning of JstfPriority table(may be NULL)
            //Offset16 extensionDisableGPOS    Offset to extension-disable JstfGPOSModList table, from beginning of JstfPriority table(may be NULL)

            public ushort extensionEnableGPOS;
            public ushort extensionDisableGPOS;

            //Offset16 extensionJstfMax        Offset to extension JstfMax table, from beginning of JstfPriority table(may be NULL)
            public ushort extensionJstfMax;
        }

        static JstfPriority ReadJstfPriority(BinaryReader reader)
        {
            //Justification Priority Table
            //A Justification Priority(JstfPriority)
            //table defines justification suggestions for a single priority level.

            //Each priority level specifies whether to enable or disable GSUB and GPOS lookups or 
            //apply text justification lookups to shrink and extend lines of text.

            //JstfPriority has offsets to four tables with line shrinkage data: 
            //two are JstfGSUBModList tables for enabling and disabling glyph substitution lookups, and 
            //two are JstfGPOSModList tables for enabling and disabling glyph positioning lookups.
            //Offsets to JstfGSUBModList and JstfGPOSModList tables also are defined for line extension.

            return new JstfPriority()
            {
                shrinkageEnableGSUB = reader.ReadUInt16(),
                shrinkageDisableGSUB = reader.ReadUInt16(),

                shrinkageEnableGPOS = reader.ReadUInt16(),
                shrinkageDisableGPOS = reader.ReadUInt16(),

                shrinkageJstfMax = reader.ReadUInt16(),

                extensionEnableGSUB = reader.ReadUInt16(),
                extensionDisableGSUB = reader.ReadUInt16(),

                extensionEnableGPOS = reader.ReadUInt16(),
                extensionDisableGPOS = reader.ReadUInt16(),

                extensionJstfMax = reader.ReadUInt16(),
            };
        }

    }
}