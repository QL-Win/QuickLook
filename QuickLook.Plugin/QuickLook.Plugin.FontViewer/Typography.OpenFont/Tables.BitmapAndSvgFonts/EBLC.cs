//MIT, 2017-present, WinterDev
//MIT, 2015, Michael Popoloski, WinterDev

using System;
using System.IO;
using Typography.OpenFont.Tables.BitmapFonts;

namespace Typography.OpenFont.Tables
{
    /// <summary>
    /// EBLC : Embedded bitmap location data
    /// </summary>
    class EBLC : TableEntry
    {
        public const string _N = "EBLC";
        public override string Name => _N;
        //
        //from https://docs.microsoft.com/en-us/typography/opentype/spec/eblc
        //EBLC - Embedded Bitmap Location Table
        //----------------------------------------------
        //The EBLC provides embedded bitmap locators.It is used together with the EDBTtable, which provides embedded, monochrome or grayscale bitmap glyph data, and the EBSC table, which provided embedded bitmap scaling information.
        //OpenType embedded bitmaps are called 'sbits' (for “scaler bitmaps”). A set of bitmaps for a face at a given size is called a strike.
        //The 'EBLC' table identifies the sizes and glyph ranges of the sbits, and keeps offsets to glyph bitmap data in indexSubTables.The 'EBDT' table then stores the glyph bitmap data, also in a number of different possible formats.Glyph metrics information may be stored in either the 'EBLC' or 'EBDT' table, depending upon the indexSubTable and glyph bitmap formats. The 'EBSC' table identifies sizes that will be handled by scaling up or scaling down other sbit sizes.
        //The 'EBLC' table uses the same format as the Apple Apple Advanced Typography (AAT) 'bloc' table.
        //The 'EBLC' table begins with a header containing the table version and number of strikes.An OpenType font may have one or more strikes embedded in the 'EBDT' table.
        //----------------------------------------------
        //eblcHeader 
        //----------------------------------------------
        //Type      Name            Description
        //uint16    majorVersion    Major version of the EBLC table, = 2.
        //uint16    minorVersion    Minor version of the EBLC table, = 0.
        //uint32    numSizes        Number of bitmapSizeTables
        //----------------------------------------------
        //Note that the first version of the EBLC table is 2.0.
        //The eblcHeader is followed immediately by the bitmapSizeTable array(s). 
        //The numSizes in the eblcHeader indicates the number of bitmapSizeTables in the array.
        //Each strike is defined by one bitmapSizeTable.

        BitmapSizeTable[] _bmpSizeTables;
        protected override void ReadContentFrom(BinaryReader reader)
        {
            // load each strike table
            long eblcBeginPos = reader.BaseStream.Position;
            //
            ushort versionMajor = reader.ReadUInt16();
            ushort versionMinor = reader.ReadUInt16();
            uint numSizes = reader.ReadUInt32();

            if (numSizes > MAX_BITMAP_STRIKES)
                throw new Exception("Too many bitmap strikes in font.");

            //----------------
            var bmpSizeTables = new BitmapSizeTable[numSizes]; 
            for (int i = 0; i < numSizes; i++)
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
                    reader.BaseStream.Position = eblcBeginPos + bmpSizeTable.indexSubTableArrayOffset + indexSubTableArr.additionalOffsetToIndexSubtable;

                    subTables[i] = IndexSubTableBase.CreateFrom(bmpSizeTable, reader);
                }
            }
        }



        const int MAX_BITMAP_STRIKES = 1024;
    }
}