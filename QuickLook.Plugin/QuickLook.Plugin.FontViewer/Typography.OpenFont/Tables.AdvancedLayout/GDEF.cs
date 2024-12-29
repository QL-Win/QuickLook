//Apache2, 2016-present, WinterDev
using System;
using System.IO;

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//from https://www.microsoft.com/typography/developers/opentype/detail.htm
//GDEF Table
//As discussed in Part One, 
//the most important tables for glyph processing are GSUB and GPOS, 
//but both these tables make use of data in the Glyph Definition table.
//The GDEF table contains three kinds of information in subtables:
//1. glyph class definitions that classify different types of glyphs in a font;  
//2. attachment point lists that identify glyph positioning attachments for each glyph; 
//and 3. ligature caret lists that provide information for caret positioning and text selection involving ligatures.

//The Glyph Class Definition subtable identifies 
//four glyph classes: 
//1. simple glyphs,
//2. ligature glyphs (glyphs representing two or more glyph components), 
//3. combining mark glyphs (glyphs that combine with other classes), 
//and 4. glyph components (glyphs that represent individual parts of ligature glyphs). 

//These classes are used by both GSUB and GPOS to differentiate glyphs in a string; for example,
//to distinguish between a base vowel (simple glyph)
//and the accent (combining mark glyph) that a GPOS feature will position above it.

//The Attachment Point List 
//identifies all the glyph attachment points defined in the GPOS table. 
//Clients that access this information in the GDEF table can cache attachment coordinates with the rasterized glyph bitmaps, 
//and avoid having to recalculate the attachment points each time they display a glyph. 
//Without this table, 
//GPOS features could still be enabled, 
//but processing speed would be slower because the client would need to decode the GPOS lookups
//that define the attachment points and compile its own list.

//The Ligature Caret List
//defines the positions for the caret to occupy in ligatures. 
//This information, which can be fine tuned for particular bitmap sizes,
//makes it possible for the caret to step across the component characters of a ligature, and for the user to select text including parts of ligatures. 
//In the example on the left, below, the caret is positioned between two components of a ligature; on the right, text is selected from within a ligature. 
//

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//https://docs.microsoft.com/en-us/typography/opentype/spec/gdef

//GDEF — Glyph Definition Table

//The Glyph Definition (GDEF) table contains six types of information in six independent tables:

//    The GlyphClassDef table classifies the different types of glyphs in the font.
//    The AttachmentList table identifies all attachment points on the glyphs, which streamlines data access and bitmap caching.
//    The LigatureCaretList table contains positioning data for ligature carets, which the text-processing client uses on screen to select and highlight the individual components of a ligature glyph.
//    The MarkAttachClassDef table classifies mark glyphs, to help group together marks that are positioned similarly.
//    The MarkGlyphSetsTable allows the enumeration of an arbitrary number of glyph sets that can be used as an extension of the mark attachment class definition to allow lookups to filter mark glyphs by arbitrary sets of marks.
//    The ItemVariationStore table is used in variable fonts to contain variation data used for adjustment of values in the GDEF, GPOS or JSTF tables.

//The GSUB and GPOS tables may reference certain GDEF table information used for processing of lookup tables. See, for example, the LookupFlag bit enumeration in “OpenType Layout Common Table Formats”.

//In variable fonts, the GDEF, GPOS and JSTF tables may all reference variation data within the ItemVariationStore table contained within the GDEF table. See below for further discussion of variable fonts and the ItemVariationStore table.
///////////////////////////////////////////////////////////////////////////////


namespace Typography.OpenFont.Tables
{

    class GDEF : TableEntry
    {
        public const string _N = "GDEF";
        public override string Name => _N;
        //
        long _tableStartAt;
        protected override void ReadContentFrom(BinaryReader reader)
        {
            _tableStartAt = reader.BaseStream.Position;
            //-----------------------------------------
            //GDEF Header, Version 1.0
            //Type 	    Name 	            Description
            //uint16 	MajorVersion 	    Major version of the GDEF table, = 1
            //uint16 	MinorVersion 	    Minor version of the GDEF table, = 0
            //Offset16 	GlyphClassDef 	    Offset to class definition table for glyph type, from beginning of GDEF header (may be NULL)
            //Offset16 	AttachList  	    Offset to list of glyphs with attachment points, from beginning of GDEF header (may be NULL)
            //Offset16 	LigCaretList 	    Offset to list of positioning points for ligature carets, from beginning of GDEF header (may be NULL)
            //Offset16 	MarkAttachClassDef 	Offset to class definition table for mark attachment type, from beginning of GDEF header (may be NULL)
            //
            //GDEF Header, Version 1.2
            //Type 	Name 	Description
            //uint16 	MajorVersion 	    Major version of the GDEF table, = 1
            //uint16 	MinorVersion 	    Minor version of the GDEF table, = 2
            //Offset16 	GlyphClassDef 	    Offset to class definition table for glyph type, from beginning of GDEF header (may be NULL)
            //Offset16 	AttachList 	        Offset to list of glyphs with attachment points, from beginning of GDEF header (may be NULL)
            //Offset16 	LigCaretList 	    Offset to list of positioning points for ligature carets, from beginning of GDEF header (may be NULL)
            //Offset16 	MarkAttachClassDef 	Offset to class definition table for mark attachment type, from beginning of GDEF header (may be NULL)
            //Offset16 	MarkGlyphSetsDef 	Offset to the table of mark set definitions, from beginning of GDEF header (may be NULL)
            //
            //GDEF Header, Version 1.3
            //Type 	Name 	Description
            //uint16 	MajorVersion 	    Major version of the GDEF table, = 1
            //uint16 	MinorVersion 	    Minor version of the GDEF table, = 3
            //Offset16 	GlyphClassDef 	    Offset to class definition table for glyph type, from beginning of GDEF header (may be NULL)
            //Offset16 	AttachList  	    Offset to list of glyphs with attachment points, from beginning of GDEF header (may be NULL)
            //Offset16 	LigCaretList 	    Offset to list of positioning points for ligature carets, from beginning of GDEF header (may be NULL)
            //Offset16 	MarkAttachClassDef 	Offset to class definition table for mark attachment type, from beginning of GDEF header (may be NULL)
            //Offset16 	MarkGlyphSetsDef 	Offset to the table of mark set definitions, from beginning of GDEF header (may be NULL)
            //Offset32 	ItemVarStore 	    Offset to the Item Variation Store table, from beginning of GDEF header (may be NULL)

            //common to 1.0, 1.2, 1.3...
            this.MajorVersion = reader.ReadUInt16();
            this.MinorVersion = reader.ReadUInt16();
            //
            ushort glyphClassDefOffset = reader.ReadUInt16();
            ushort attachListOffset = reader.ReadUInt16();
            ushort ligCaretListOffset = reader.ReadUInt16();
            ushort markAttachClassDefOffset = reader.ReadUInt16();
            ushort markGlyphSetsDefOffset = 0;
            uint itemVarStoreOffset = 0;
            //
            switch (MinorVersion)
            {
                default:
                    Utils.WarnUnimplemented("GDEF Minor Version {0}", MinorVersion);
                    return;
                case 0:
                    break;
                case 2:
                    markGlyphSetsDefOffset = reader.ReadUInt16();
                    break;
                case 3:
                    markGlyphSetsDefOffset = reader.ReadUInt16();
                    itemVarStoreOffset = reader.ReadUInt32();
                    break;
            }
            //---------------


            this.GlyphClassDef = (glyphClassDefOffset == 0) ? null : ClassDefTable.CreateFrom(reader, _tableStartAt + glyphClassDefOffset);
            this.AttachmentListTable = (attachListOffset == 0) ? null : AttachmentListTable.CreateFrom(reader, _tableStartAt + attachListOffset);
            this.LigCaretList = (ligCaretListOffset == 0) ? null : LigCaretList.CreateFrom(reader, _tableStartAt + ligCaretListOffset);

            //A Mark Attachment Class Definition Table defines the class to which a mark glyph may belong.
            //This table uses the same format as the Class Definition table (for details, see the chapter, Common Table Formats ).


#if DEBUG
            if (markAttachClassDefOffset == 2)
            {
                //temp debug invalid font                
                this.MarkAttachmentClassDef = (markAttachClassDefOffset == 0) ? null : ClassDefTable.CreateFrom(reader, reader.BaseStream.Position);
            }
            else
            {
                this.MarkAttachmentClassDef = (markAttachClassDefOffset == 0) ? null : ClassDefTable.CreateFrom(reader, _tableStartAt + markAttachClassDefOffset);
            }
#else
            this.MarkAttachmentClassDef = (markAttachClassDefOffset == 0) ? null : ClassDefTable.CreateFrom(reader, _tableStartAt + markAttachClassDefOffset);
#endif

            this.MarkGlyphSetsTable = (markGlyphSetsDefOffset == 0) ? null : MarkGlyphSetsTable.CreateFrom(reader, _tableStartAt + markGlyphSetsDefOffset);

            if (itemVarStoreOffset != 0)
            {
                //not supported
                Utils.WarnUnimplemented("GDEF ItemVarStore");
                reader.BaseStream.Seek(this.Header.Offset + itemVarStoreOffset, SeekOrigin.Begin);
            }
        }
        public int MajorVersion { get; private set; }
        public int MinorVersion { get; private set; }
        public ClassDefTable GlyphClassDef { get; private set; }
        public AttachmentListTable AttachmentListTable { get; private set; }
        public LigCaretList LigCaretList { get; private set; }
        public ClassDefTable MarkAttachmentClassDef { get; private set; }
        public MarkGlyphSetsTable MarkGlyphSetsTable { get; private set; }

        //------------------------
        /// <summary>
        /// fill gdef to each glyphs
        /// </summary>
        /// <param name="inputGlyphs"></param>
        public void FillGlyphData(Glyph[] inputGlyphs)
        {
            //1. 
            FillClassDefs(inputGlyphs);
            //2. 
            FillAttachPoints(inputGlyphs);
            //3.
            FillLigatureCarets(inputGlyphs);
            //4.
            FillMarkAttachmentClassDefs(inputGlyphs);
            //5.
            FillMarkGlyphSets(inputGlyphs);
        }
        void FillClassDefs(Glyph[] inputGlyphs)
        {
            //1. glyph def 
            ClassDefTable classDef = GlyphClassDef;
            if (classDef == null) return;
            //-----------------------------------------

            switch (classDef.Format)
            {
                default:
                    Utils.WarnUnimplemented("GDEF GlyphClassDef Format {0}", classDef.Format);
                    break;
                case 1:
                    {
                        ushort startGlyph = classDef.startGlyph;
                        ushort[] classValues = classDef.classValueArray;
                        int gIndex = startGlyph;
                        for (int i = 0; i < classValues.Length; ++i)
                        {
#if DEBUG
                            ushort classV = classValues[i];
                            if (classV > (ushort)GlyphClassKind.Component)
                            {

                            }
#endif

                            inputGlyphs[gIndex].GlyphClass = (GlyphClassKind)classValues[i];
                            gIndex++;
                        }

                    }
                    break;
                case 2:
                    {
                        ClassDefTable.ClassRangeRecord[] records = classDef.records;
                        for (int n = 0; n < records.Length; ++n)
                        {
                            ClassDefTable.ClassRangeRecord rec = records[n];

#if DEBUG

                            if (rec.classNo > (ushort)GlyphClassKind.Component)
                            {

                            }
#endif

                            GlyphClassKind glyphKind = (GlyphClassKind)rec.classNo;
                            for (int i = rec.startGlyphId; i <= rec.endGlyphId; ++i)
                            {
                                inputGlyphs[i].GlyphClass = glyphKind;
                            }
                        }
                    }
                    break;
            }
        }
        void FillAttachPoints(Glyph[] inputGlyphs)
        {
            AttachmentListTable attachmentListTable = this.AttachmentListTable;
            if (attachmentListTable == null) { return; }
            //-----------------------------------------

            Utils.WarnUnimplemented("please implement GDEF.FillAttachPoints()");
        }
        void FillLigatureCarets(Glyph[] inputGlyphs)
        {
            //Console.WriteLine("please implement FillLigatureCarets()");
        }
        void FillMarkAttachmentClassDefs(Glyph[] inputGlyphs)
        {
            //Mark Attachment Class Definition Table
            //A Mark Class Definition Table is used to assign mark glyphs into different classes 
            //that can be used in lookup tables within the GSUB or GPOS table to control how mark glyphs within a glyph sequence are treated by lookups.
            //For more information on the use of mark attachment classes, 
            //see the description of lookup flags in the “Lookup Table” section of the chapter, OpenType Layout Common Table Formats.
            ClassDefTable markAttachmentClassDef = this.MarkAttachmentClassDef;
            if (markAttachmentClassDef == null) return;
            //-----------------------------------------

            switch (markAttachmentClassDef.Format)
            {
                default:
                    Utils.WarnUnimplemented("GDEF MarkAttachmentClassDef Table Format {0}", markAttachmentClassDef.Format);
                    break;
                case 1:
                    {
                        ushort startGlyph = markAttachmentClassDef.startGlyph;
                        ushort[] classValues = markAttachmentClassDef.classValueArray;

                        int len = classValues.Length;
                        int gIndex = startGlyph;
                        for (int i = 0; i < len; ++i)
                        {
#if DEBUG
                            Glyph dbugTestGlyph = inputGlyphs[gIndex];
#endif
                            inputGlyphs[gIndex].MarkClassDef = classValues[i];
                            gIndex++;
                        }

                    }
                    break;
                case 2:
                    {
                        ClassDefTable.ClassRangeRecord[] records = markAttachmentClassDef.records;
                        int len = records.Length;
                        for (int n = 0; n < len; ++n)
                        {
                            ClassDefTable.ClassRangeRecord rec = records[n];
                            for (int i = rec.startGlyphId; i <= rec.endGlyphId; ++i)
                            {
#if DEBUG
                                Glyph dbugTestGlyph = inputGlyphs[i];
#endif
                                inputGlyphs[i].MarkClassDef = rec.classNo;
                            }
                        }
                    }
                    break;
            }
        }
        void FillMarkGlyphSets(Glyph[] inputGlyphs)
        {
            //Mark Glyph Sets Table
            //A Mark Glyph Sets table is used to define sets of mark glyphs that can be used in lookup tables within the GSUB or GPOS table to control 
            //how mark glyphs within a glyph sequence are treated by lookups. For more information on the use of mark glyph sets,
            //see the description of lookup flags in the “Lookup Table” section of the chapter, OpenType Layout Common Table Formats.
            MarkGlyphSetsTable markGlyphSets = this.MarkGlyphSetsTable;
            if (markGlyphSets == null) return;
            //-----------------------------------------

            Utils.WarnUnimplemented("please implement GDEF.FillMarkGlyphSets()");
        }
    }
}
