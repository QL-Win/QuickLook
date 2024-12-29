//MIT, 2019-present, WinterDev
using System;
using System.Collections.Generic;
using System.IO;

using Typography.OpenFont.Tables.BitmapFonts;

namespace Typography.OpenFont.Tables
{
    //test font=> NotoColorEmoji.ttf

    //from https://docs.microsoft.com/en-us/typography/opentype/spec/cblc

    //Table structure

    //The CBLC table provides embedded bitmap locators.
    //It is used together with the CBDT table, which provides embedded, 
    //color bitmap glyph data.
    //The formats of these two tables are backward compatible with the EBDT and EBLC tables
    //used for embedded monochrome and grayscale bitmaps.

    //The CBLC table begins with a header containing the table version and number of strikes.
    //CblcHeader
    //Type      Name            Description
    //uint16    majorVersion    Major version of the CBLC table, = 3.
    //uint16    minorVersion    Minor version of the CBLC table, = 0.
    //uint32    numSizes        Number of BitmapSize tables

    //Note that the first version of the CBLC table is 3.0.

    //The CblcHeader is followed immediately by the BitmapSize table array(s). 
    //The numSizes in the CblcHeader indicates the number of BitmapSize tables in the array.
    //Each strike is defined by one BitmapSize table.

    /// <summary>
    /// embeded bitmap locator
    /// </summary>
    class CBLC : TableEntry
    {
        BitmapSizeTable[] _bmpSizeTables;

        public const string _N = "CBLC";
        public override string Name => _N;

        protected override void ReadContentFrom(BinaryReader reader)
        {
            long cblcBeginPos = reader.BaseStream.Position;
            ushort majorVersion = reader.ReadUInt16(); //3
            ushort minorVersion = reader.ReadUInt16(); //0
            uint numSizes = reader.ReadUInt32();

            //The CblcHeader is followed immediately by the BitmapSize table array(s). 
            //The numSizes in the CblcHeader indicates the number of BitmapSize tables in the array. 
            //Each strike is defined by one BitmapSize table.
            BitmapSizeTable[] bmpSizeTables = new BitmapSizeTable[numSizes];
            for (int i = 0; i < numSizes; ++i)
            {
                bmpSizeTables[i] = BitmapSizeTable.ReadBitmapSizeTable(reader);
            }
            _bmpSizeTables = bmpSizeTables;

            // 
            //-------
            //IndexSubTableArray
            //Type      Name            Description
            //uint16    firstGlyphIndex First glyph ID of this range.
            //uint16    lastGlyphIndex  Last glyph ID of this range(inclusive).
            //Offset32  additionalOffsetToIndexSubtable     Add to indexSubTableArrayOffset to get offset from beginning of EBLC.

            //After determining the strike,
            //the rasterizer searches this array for the range containing the given glyph ID.
            //When the range is found, the additionalOffsetToIndexSubtable is added to the indexSubTableArrayOffset
            //to get the offset of the IndexSubTable in the EBLC.

            //The first indexSubTableArray is located after the last bitmapSizeSubTable entry.
            //Then the IndexSubTables for the strike follow.
            //Another IndexSubTableArray(if more than one strike) and 
            //its IndexSubTableArray are next.

            //The EBLC continues with an array and IndexSubTables for each strike.
            //We now have the offset to the IndexSubTable.
            //All IndexSubTable formats begin with an IndexSubHeader which identifies the IndexSubTable format, 
            //the format of the EBDT image data,
            //and the offset from the beginning of the EBDT table to the beginning of the image data for this range.

            for (int n = 0; n < numSizes; ++n)
            {
                BitmapSizeTable bmpSizeTable = bmpSizeTables[n];
                uint numberofIndexSubTables = bmpSizeTable.numberOfIndexSubTables;

                //
                IndexSubTableArray[] indexSubTableArrs = new IndexSubTableArray[numberofIndexSubTables];
                for (uint i = 0; i < numberofIndexSubTables; ++i)
                {
                    indexSubTableArrs[i] = new IndexSubTableArray(
                             reader.ReadUInt16(), //First glyph ID of this range.
                             reader.ReadUInt16(), //Last glyph ID of this range (inclusive).
                             reader.ReadUInt32());//Add to indexSubTableArrayOffset to get offset from beginning of EBLC.                      
                }

                //---
                IndexSubTableBase[] subTables = new IndexSubTableBase[numberofIndexSubTables];
                bmpSizeTable.indexSubTables = subTables;
                for (uint i = 0; i < numberofIndexSubTables; ++i)
                {
                    IndexSubTableArray indexSubTableArr = indexSubTableArrs[i];
                    reader.BaseStream.Position = cblcBeginPos + bmpSizeTable.indexSubTableArrayOffset + indexSubTableArr.additionalOffsetToIndexSubtable;

                    IndexSubTableBase result = subTables[i] = IndexSubTableBase.CreateFrom(bmpSizeTable, reader);
                    result.firstGlyphIndex = indexSubTableArr.firstGlyphIndex;
                    result.lastGlyphIndex = indexSubTableArr.lastGlyphIndex;
                }
            }
        }
        public Glyph[] BuildGlyphList()
        {
            List<Glyph> glyphs = new List<Glyph>();
            int numSizes = _bmpSizeTables.Length;
            for (int n = 0; n < numSizes; ++n)
            {
                BitmapSizeTable bmpSizeTable = _bmpSizeTables[n];
                uint numberofIndexSubTables = bmpSizeTable.numberOfIndexSubTables;
                for (uint i = 0; i < numberofIndexSubTables; ++i)
                {
                    bmpSizeTable.indexSubTables[i].BuildGlyphList(glyphs);
                }
            }
            return glyphs.ToArray();
        }
    }


}