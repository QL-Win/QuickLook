//MIT, 2019-present, WinterDev
using System;
using System.IO;

namespace Typography.OpenFont.Tables
{
    //from https://docs.microsoft.com/en-us/typography/opentype/spec/ebdt

    //The EBDT table is used to embed monochrome or grayscale bitmap glyph data.
    //It is used together with the EBLC table,
    //which provides embedded bitmap locators, 
    //and the EBSC table, which provides embedded bitmap scaling information.

    //OpenType embedded bitmaps are also called “sbits” (for “scaler bitmaps”). 
    //A set of bitmaps for a face at a given size is called a strike.

    //The EBLC table identifies the sizes and glyph ranges of the sbits, 
    //and keeps offsets to glyph bitmap data in indexSubTables.

    //The EBDT table then stores the glyph bitmap data,
    //in a number of different possible formats.
    //Glyph metrics information may be stored in either the EBLC or EBDT table, 
    //depending upon the indexSubTable and glyph bitmap data formats.

    //The EBSC table identifies sizes that will be handled by scaling up or scaling down other sbit sizes.


    //The EBDT table is a super set of Apple’s Apple Advanced Typography (AAT) 'bdat' table.


    /// <summary>
    ///  Embedded Bitmap Data Table
    /// </summary>
    class EBDT : TableEntry
    {
        public const string _N = "EBDT";
        public override string Name => _N;

        protected override void ReadContentFrom(BinaryReader reader)
        {
            ushort majorVersion = reader.ReadUInt16();
            ushort minorVersion = reader.ReadUInt16();

            //The rest of the EBDT table is a collection of bitmap data.
            //The data can be in a number of possible formats, 
            //indicated by information in the EBLC table.

            //Some of the formats contain metric information plus image data,
            //and other formats contain only the image data.
            //Long word alignment is not required for these sub tables;
            //byte alignment is sufficient.

            //There are also two different formats for glyph metrics:
            //big glyph metrics and small glyph metrics.
            //Big glyph metrics define metrics information 
            //for both horizontal and vertical layouts.
            //This is important in fonts(such as Kanji) where both types of layout may be used.
            //Small glyph metrics define metrics information for one layout direction only.
            //Which direction applies, horizontal or vertical, is determined by the flags field in the BitmapSize 
            //tables within the EBLC table.

        }
    }
}