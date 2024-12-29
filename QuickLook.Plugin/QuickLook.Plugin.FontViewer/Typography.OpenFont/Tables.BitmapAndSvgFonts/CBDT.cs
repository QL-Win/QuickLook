//MIT, 2019-present, WinterDev
using System;
using System.IO;
using Typography.OpenFont.Tables.BitmapFonts;

namespace Typography.OpenFont.Tables
{
    //test font=> NotoColorEmoji.ttf

    //from https://docs.microsoft.com/en-us/typography/opentype/spec/cbdt

    //Table structure

    //The CBDT table is used to embed color bitmap glyph data. It is used together with the CBLC table,
    //which provides embedded bitmap locators.
    //The formats of these two tables are backward compatible with the EBDT and EBLC tables
    //used for embedded monochrome and grayscale bitmaps.

    //The CBDT table begins with a header containing simply the table version number.
    //Type 	    Name 	        Description
    //uint16 	majorVersion 	Major version of the CBDT table, = 3.
    //uint16 	minorVersion 	Minor version of the CBDT table, = 0.

    //Note that the first version of the CBDT table is 3.0.

    //The rest of the CBDT table is a collection of bitmap data.
    //The data can be presented in three possible formats,
    //indicated by information in the CBLC table.
    //Some of the formats contain metric information plus image data, 
    //and other formats contain only the image data. Long word alignment is not required for these subtables;
    //byte alignment is sufficient.

    class CBDT : TableEntry, IDisposable
    {
        public const string _N = "CBDT";
        public override string Name => _N;

        readonly GlyphBitmapDataFmt17 _format17 = new GlyphBitmapDataFmt17();
        readonly GlyphBitmapDataFmt18 _format18 = new GlyphBitmapDataFmt18();
        readonly GlyphBitmapDataFmt19 _format19 = new GlyphBitmapDataFmt19();


        System.IO.MemoryStream _ms; //sub-stream contains image data
        Typography.OpenFont.ByteOrderSwappingBinaryReader _binReader;

        public void Dispose()
        {
            RemoveOldMemoryStreamAndReaders();
        }

        public void RemoveOldMemoryStreamAndReaders()
        {
            try
            {
                if (_binReader != null)
                {
                    ((System.IDisposable)_binReader).Dispose();
                    _binReader = null;
                }
                if (_ms != null)
                {
                    _ms.Dispose();
                    _ms = null;
                }
            }
            catch (Exception ex)
            {
                //
            }
        }
        protected override void ReadContentFrom(BinaryReader reader)
        {

            //we copy data from the input mem stream
            //and store inside this table for later use.
            RemoveOldMemoryStreamAndReaders();

            //-------------------
            byte[] data = reader.ReadBytes((int)this.Header.Length);//***
            _ms = new MemoryStream(data);
            _binReader = new ByteOrderSwappingBinaryReader(_ms);
        }
        public void FillGlyphInfo(Glyph glyph)
        {
            //int srcOffset, int srcLen, int srcFormat,
            _binReader.BaseStream.Position = glyph.BitmapStreamOffset;
            switch (glyph.BitmapFormat)
            {
                case 17: _format17.FillGlyphInfo(_binReader, glyph); break;
                case 18: _format18.FillGlyphInfo(_binReader, glyph); break;
                case 19: _format19.FillGlyphInfo(_binReader, glyph); break;
                default:
                    throw new OpenFontNotSupportedException();
            }
        }
        public void CopyBitmapContent(Glyph glyph, System.IO.Stream outputStream)
        {
            //1 
            _binReader.BaseStream.Position = glyph.BitmapStreamOffset;
            switch (glyph.BitmapFormat)
            {
                case 17: _format17.ReadRawBitmap(_binReader, glyph, outputStream); break;
                case 18: _format18.ReadRawBitmap(_binReader, glyph, outputStream); break;
                case 19: _format19.ReadRawBitmap(_binReader, glyph, outputStream); break;
                default:
                    throw new OpenFontNotSupportedException();
            }
        }
    }
}