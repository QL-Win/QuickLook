//MIT, 2019-present, WinterDev
using System;

namespace Typography.OpenFont.Tables
{
    class BitmapFontGlyphSource
    {
        readonly CBLC _cblc; //bitmap locator
        CBDT _cbdt;
        public BitmapFontGlyphSource(CBLC cblc) => _cblc = cblc;

        /// <summary>
        /// load new bitmap embeded data
        /// </summary>
        /// <param name="cbdt"></param>
        public void LoadCBDT(CBDT cbdt) => _cbdt = cbdt;

        /// <summary>
        /// clear and remove existing bitmap embeded data
        /// </summary>
        public void UnloadCBDT()
        {
            if (_cbdt != null)
            {
                _cbdt.RemoveOldMemoryStreamAndReaders();
                _cbdt = null;
            }
        }

        public void CopyBitmapContent(Glyph glyph, System.IO.Stream outputStream) => _cbdt.CopyBitmapContent(glyph, outputStream);

        public Glyph[] BuildGlyphList()
        {
            Glyph[] glyphs = _cblc.BuildGlyphList();
            for (int i = 0; i < glyphs.Length; ++i)
            {
                _cbdt.FillGlyphInfo(glyphs[i]);
            }
            return glyphs;
        }
    }
}