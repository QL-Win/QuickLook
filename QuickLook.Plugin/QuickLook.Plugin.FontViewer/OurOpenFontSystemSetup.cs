//MIT, 2016-present, WinterDev
using System;
using System.IO;

using Typography.OpenFont.WebFont;
using BrotliSharpLib;
using System.IO.Compression;

namespace SampleWinForms;

public static class OurOpenFontSystemSetup
{
    public static void SetupWoffDecompressFunctions()
    {
        //
        //Woff
        //        WoffDefaultZlibDecompressFunc.DecompressHandler = (byte[] compressedBytes, byte[] decompressedResult) =>
        //        {
        //            //ZLIB
        //            //****
        //            //YOU can change to  your prefer decode libs***
        //            //****

        //            bool result = false;
        //            try
        //            {
        //                var inflater = new ICSharpCode.SharpZipLib.Zip.Compression.Inflater();
        //                inflater.SetInput(compressedBytes);
        //                inflater.Inflate(decompressedResult);
        //#if DEBUG
        //                long outputLen = inflater.TotalOut;
        //                if (outputLen != decompressedResult.Length)
        //                {
        //                }
        //#endif

        //                result = true;
        //            }
        //            catch (Exception ex)
        //            {
        //            }
        //            return result;
        //        };
        //Woff2

        Woff2DefaultBrotliDecompressFunc.DecompressHandler = (byte[] compressedBytes, Stream output) =>
        {
            //BROTLI
            //****
            //YOU can change to  your prefer decode libs***
            //****

            bool result = false;
            try
            {
                using (var ms = new MemoryStream(compressedBytes))
                {
                    ms.Position = 0;//set to start pos
                    Decompress(ms, output);
                }
                result = true;
            }
            catch (Exception ex)
            {
            }
            return result;
        };
    }

    static void Decompress(Stream input, Stream output)
    {
        try
        {
            using (var bs = new BrotliStream(input, CompressionMode.Decompress))
            using (var ms = new MemoryStream())
            {
                bs.CopyTo(output);
            }
        }
        catch (IOException ex)
        {
            throw ex;
        }
    }
}
