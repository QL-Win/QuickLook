//Apache2, 2016-present, WinterDev

using System.IO;

namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2
    //Attachment List Table

    //The Attachment List table (AttachList) may be used to cache attachment point coordinates along with glyph bitmaps.

    //The table consists of an offset to a Coverage table (Coverage) listing all glyphs that define attachment points in the GPOS table,
    //a count of the glyphs with attachment points (GlyphCount), and an array of offsets to AttachPoint tables (AttachPoint). 
    //The array lists the AttachPoint tables, one for each glyph in the Coverage table, in the same order as the Coverage Index.
    //AttachList table
    //Type 	    Name 	        Description
    //Offset16 	Coverage 	    Offset to Coverage table - from beginning of AttachList table
    //unint16 	GlyphCount 	    Number of glyphs with attachment points
    //Offset16 	AttachPoint[GlyphCount] 	Array of offsets to AttachPoint tables-from beginning of AttachList table-in Coverage Index order

    //An AttachPoint table consists of a count of the attachment points on a single glyph (PointCount) and 
    //an array of contour indices of those points (PointIndex), listed in increasing numerical order.

    //Example 3 at the end of the chapter demonstrates an AttachList table that defines attachment points for two glyphs.
    //AttachPoint table
    //Type 	    Name 	    Description
    //uint16 	PointCount 	Number of attachment points on this glyph
    //uint16 	PointIndex[PointCount] 	Array of contour point indices -in increasing numerical order

    class AttachmentListTable
    {
        AttachPoint[] _attachPoints;
        public CoverageTable CoverageTable { get; private set; }
        public static AttachmentListTable CreateFrom(BinaryReader reader, long beginAt)
        {
            AttachmentListTable attachmentListTable = new AttachmentListTable();
            reader.BaseStream.Seek(beginAt, SeekOrigin.Begin);
            //
            ushort coverageOffset = reader.ReadUInt16();
            ushort glyphCount = reader.ReadUInt16();
            ushort[] attachPointOffsets = Utils.ReadUInt16Array(reader, glyphCount);
            //-----------------------
            attachmentListTable.CoverageTable = CoverageTable.CreateFrom(reader, beginAt + coverageOffset);
            attachmentListTable._attachPoints = new AttachPoint[glyphCount];
            for (int i = 0; i < glyphCount; ++i)
            {
                reader.BaseStream.Seek(beginAt + attachPointOffsets[i], SeekOrigin.Begin);
                ushort pointCount = reader.ReadUInt16();
                attachmentListTable._attachPoints[i] = new AttachPoint()
                {
                    pointIndices = Utils.ReadUInt16Array(reader, pointCount)
                };
            }

            return attachmentListTable;
        }
        struct AttachPoint
        {
            public ushort[] pointIndices;
        }
    }

}