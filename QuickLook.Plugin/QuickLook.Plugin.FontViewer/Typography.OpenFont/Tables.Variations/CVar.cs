//MIT, 2019-present, WinterDev
using System;
using System.IO;

namespace Typography.OpenFont.Tables
{
    //https://docs.microsoft.com/en-us/typography/opentype/spec/cvar

    /// <summary>
    /// cvar — CVT Variations Table
    /// </summary>
    class CVar : TableEntry
    {
        public const string _N = "cvar";
        public override string Name => _N;
        public CVar()
        {
            //The control value table (CVT) variations table is used in variable fonts to provide variation data for CVT values.
            //For a general overview of OpenType Font Variations


        }
        protected override void ReadContentFrom(BinaryReader reader)
        {

        }
    }
}