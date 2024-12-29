//MIT, 2019-present, WinterDev
using System;
using System.IO;

namespace Typography.OpenFont.Tables
{

    //from 
    //https://docs.microsoft.com/en-us/typography/opentype/spec/ebsc

    //The EBSC table provides a mechanism for describing embedded bitmaps 
    //which are created by scaling other embedded bitmaps.
    //While this is the sort of thing that outline font technologies were invented to avoid,
    //there are cases (small sizes of Kanji, for example)
    //where scaling a bitmap produces a more legible font 
    //than scan-converting an outline.

    //For this reason the EBSC table allows a font to define a bitmap strike
    //as a scaled version of another strike.

    //The EBSC table is used together with the EBDT table,
    //which provides embedded monochrome or grayscale bitmap data,
    //and the EBLC table, which provides embedded bitmap locators.


    /// <summary>
    /// EBSC — Embedded Bitmap Scaling Table
    /// </summary>
    class EBSC : TableEntry
    {
        public const string _N = "EBSC";
        public override string Name => _N;

        protected override void ReadContentFrom(BinaryReader reader)
        {

        }
    }
}
