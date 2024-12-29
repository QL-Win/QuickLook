//Apache2, 2016-present, WinterDev
using System;
using System.IO;

namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2
    //----------------------------
    //Class Definition Table
    //----------------------------
    //
    //In OpenType Layout, index values identify glyphs. For efficiency and ease of representation, a font developer can group glyph indices to form glyph classes.
    //Class assignments vary in meaning from one lookup subtable to another. 
    //For example, in the GSUB and GPOS tables, classes are used to describe glyph contexts. 
    //GDEF tables also use the idea of glyph classes.

    //Consider a substitution action that replaces only the lowercase ascender glyphs in a glyph string. 
    //To more easily describe the appropriate context for the substitution,
    //the font developer might divide the font's lowercase glyphs into two classes, 
    //one that contains the ascenders and one that contains the glyphs without ascenders.

    //A font developer can assign any glyph to any class, each identified with an integer called a class value.
    //A Class Definition table (ClassDef) groups glyph indices by class, 
    //beginning with Class 1, then Class 2, and so on.
    //All glyphs not assigned to a class fall into Class 0.
    //Within a given class definition table, each glyph in the font belongs to exactly one class.

    //The ClassDef table can have either of two formats: 
    //one that assigns a range of consecutive glyph indices to different classes,
    //or one that puts groups of consecutive glyph indices into the same class.
    //
    //
    //Class Definition Table Format 1
    //
    //
    //The first class definition format (ClassDefFormat1) specifies
    //a range of consecutive glyph indices and a list of corresponding glyph class values.
    //This table is useful for assigning each glyph to a different class because
    //the glyph indices in each class are not grouped together.

    //A ClassDef Format 1 table begins with a format identifier (ClassFormat). 
    //The range of glyph indices (GlyphIDs) covered by the table is identified by two values: the GlyphID of the first glyph (StartGlyph),
    //and the number of consecutive GlyphIDs (including the first one) that will be assigned class values (GlyphCount). 
    //The ClassValueArray lists the class value assigned to each GlyphID, starting with the class value for StartGlyph and 
    //following the same order as the GlyphIDs. 
    //Any glyph not included in the range of covered GlyphIDs automatically belongs to Class 0.

    //Example 7 at the end of this chapter uses Format 1 to assign class values to the lowercase, x-height, ascender, and descender glyphs in a font.
    //
    //----------------------------
    //ClassDefFormat1 table: Class array
    //----------------------------
    //Type 	    Name 	        Description
    //uint16 	ClassFormat 	Format identifier-format = 1
    //uint16 	StartGlyph 	    First glyph ID of the ClassValueArray
    //uint16 	GlyphCount 	    Size of the ClassValueArray
    //uint16 	ClassValueArray[GlyphCount] 	Array of Class Values-one per GlyphID   
    //----------------------------------
    //
    //
    //Class Definition Table Format 2
    //
    //
    //The second class definition format (ClassDefFormat2) defines multiple groups of glyph indices that belong to the same class.
    //Each group consists of a discrete range of glyph indices in consecutive order (ranges cannot overlap).

    //The ClassDef Format 2 table contains a format identifier (ClassFormat),
    //a count of ClassRangeRecords that define the groups and assign class values (ClassRangeCount), 
    //and an array of ClassRangeRecords ordered by the GlyphID of the first glyph in each record (ClassRangeRecord).

    //Each ClassRangeRecord consists of a Start glyph index, an End glyph index, and a Class value.
    //All GlyphIDs in a range, from Start to End inclusive,
    //constitute the class identified by the Class value. 
    //Any glyph not covered by a ClassRangeRecord is assumed to belong to Class 0.

    //Example 8 at the end of this chapter uses Format 2 to assign class values to four types of glyphs in the Arabic script.
    //---------------------------------------
    //ClassDefFormat2 table: Class ranges
    //---------------------------------------
    //Type 	    Name 	            Description
    //uint16 	ClassFormat 	    Format identifier-format = 2
    //uint16 	ClassRangeCount 	Number of ClassRangeRecords
    //struct 	ClassRangeRecord[ClassRangeCount] 	Array of ClassRangeRecords-ordered by Start GlyphID
    //---------------------------------------
    //
    //ClassRangeRecord
    //---------------------------------------
    //Type 	    Name 	            Descriptionc
    //uint16 	Start 	            First glyph ID in the range
    //uint16 	End 	            Last glyph ID in the range
    //uint16 	Class 	            Applied to all glyphs in the range
    //---------------------------------------
    class ClassDefTable
    {
        public int Format { get; internal set; }
        //----------------
        //format 1
        public ushort startGlyph;
        public ushort[] classValueArray;
        //---------------
        //format2
        public ClassRangeRecord[] records;
        public static ClassDefTable CreateFrom(BinaryReader reader, long beginAt)
        {

            reader.BaseStream.Seek(beginAt, SeekOrigin.Begin);

            //---------
            ClassDefTable classDefTable = new ClassDefTable();
            switch (classDefTable.Format = reader.ReadUInt16())
            {
                default: throw new OpenFontNotSupportedException();
                case 1:
                    {
                        classDefTable.startGlyph = reader.ReadUInt16();
                        ushort glyphCount = reader.ReadUInt16();
                        classDefTable.classValueArray = Utils.ReadUInt16Array(reader, glyphCount);
                    }
                    break;
                case 2:
                    {
                        ushort classRangeCount = reader.ReadUInt16();
                        ClassRangeRecord[] records = new ClassRangeRecord[classRangeCount];
                        for (int i = 0; i < classRangeCount; ++i)
                        {
                            records[i] = new ClassRangeRecord(
                                reader.ReadUInt16(), //start glyph id
                                reader.ReadUInt16(), //end glyph id
                                reader.ReadUInt16()); //classNo
                        }
                        classDefTable.records = records;
                    }
                    break;
            }
            return classDefTable;
        }
        internal readonly struct ClassRangeRecord
        {
            //---------------------------------------
            //
            //ClassRangeRecord
            //---------------------------------------
            //Type 	    Name 	            Descriptionc
            //uint16 	Start 	            First glyph ID in the range
            //uint16 	End 	            Last glyph ID in the range
            //uint16 	Class 	            Applied to all glyphs in the range
            //---------------------------------------
            public readonly ushort startGlyphId;
            public readonly ushort endGlyphId;
            public readonly ushort classNo;
            public ClassRangeRecord(ushort startGlyphId, ushort endGlyphId, ushort classNo)
            {
                this.startGlyphId = startGlyphId;
                this.endGlyphId = endGlyphId;
                this.classNo = classNo;
            }
#if DEBUG
            public override string ToString()
            {
                return "class=" + classNo + " [" + startGlyphId + "," + endGlyphId + "]";
            }
#endif
        }


        public int GetClassValue(ushort glyphIndex)
        {
            switch (Format)
            {
                default: throw new OpenFontNotSupportedException();
                case 1:
                    {
                        if (glyphIndex >= startGlyph &&
                            glyphIndex < classValueArray.Length)
                        {
                            return classValueArray[glyphIndex - startGlyph];
                        }
                        return -1;
                    }
                case 2:
                    {

                        for (int i = 0; i < records.Length; ++i)
                        {
                            //TODO: review a proper method here again
                            //esp. binary search
                            ClassRangeRecord rec = records[i];
                            if (rec.startGlyphId <= glyphIndex)
                            {
                                if (glyphIndex <= rec.endGlyphId)
                                {
                                    return rec.classNo;
                                }
                            }
                            else
                            {
                                return -1;//no need to go further
                            }
                        }
                        return -1;
                    }
            }
        }
    }

}