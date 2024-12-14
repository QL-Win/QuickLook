// Copyright © 2024 ema
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using QuickLook.Common.Plugin;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Windows.Size;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

/// <summary>
/// https://github.com/BrokenEvent/CsIcnsReader
/// Note: Not support the j2k format compression
/// </summary>
internal class IcnsProvider : AnimationProvider
{
    private IcnsImage[] _images;

    public IcnsProvider(Uri path, MetaProvider meta, ContextObject contextObject) : base(path, meta, contextObject)
    {
        _images = IcnsImageParser.GetImages(Path.LocalPath);
    }

    public override void Dispose()
    {
        if (_images != null)
        {
            try
            {
                foreach (var image in _images)
                {
                    image.Bitmap?.Dispose();
                }
            }
            catch
            {
                // Nothing is important
            }

            _images = null;
        }
    }

    public override Task<BitmapSource> GetRenderedFrame(int index)
    {
        if (_images == null || _images.Length <= 0)
        {
            return new Task<BitmapSource>(() => null);
        }

        IcnsImage image = _images
            .Where(image => image.Bitmap != null)
            .OrderByDescending(image => image.Bitmap.Width)
            .FirstOrDefault();

        if (image == null)
        {
            return new Task<BitmapSource>(() => null);
        }

        using (var memoryStream = new MemoryStream())
        {
            image.Bitmap.Save(memoryStream, ImageFormat.Png);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var bs = new BitmapImage();
            bs.BeginInit();
            bs.StreamSource = memoryStream;
            bs.CacheOption = BitmapCacheOption.OnLoad;
            bs.EndInit();
            bs.Freeze();

            return new Task<BitmapSource>(() => bs);
        }
    }

    public override Task<BitmapSource> GetThumbnail(Size renderSize)
    {
        // Not implementing thumbnail method
        return GetRenderedFrame(0);
    }
}

internal static class IcnsDecoder
{
    private static readonly uint[] PALETE_4BPP =
    [
      0xffffffff, 0xfffcf305, 0xffff6402, 0xffdd0806,
      0xfff20884, 0xff4600a5, 0xff0000d4, 0xff02abea,
      0xff1fb714, 0xff006411, 0xff562c05, 0xff90713a,
      0xffc0c0c0, 0xff808080, 0xff404040, 0xff000000
    ];

    private static readonly uint[] PALETTE_8BPP =
    [
        0xFFFFFFFF, 0xFFFFFFCC, 0xFFFFFF99, 0xFFFFFF66,
        0xFFFFFF33, 0xFFFFFF00, 0xFFFFCCFF, 0xFFFFCCCC,
        0xFFFFCC99, 0xFFFFCC66, 0xFFFFCC33, 0xFFFFCC00,
        0xFFFF99FF, 0xFFFF99CC, 0xFFFF9999, 0xFFFF9966,
        0xFFFF9933, 0xFFFF9900, 0xFFFF66FF, 0xFFFF66CC,
        0xFFFF6699, 0xFFFF6666, 0xFFFF6633, 0xFFFF6600,
        0xFFFF33FF, 0xFFFF33CC, 0xFFFF3399, 0xFFFF3366,
        0xFFFF3333, 0xFFFF3300, 0xFFFF00FF, 0xFFFF00CC,
        0xFFFF0099, 0xFFFF0066, 0xFFFF0033, 0xFFFF0000,
        0xFFCCFFFF, 0xFFCCFFCC, 0xFFCCFF99, 0xFFCCFF66,
        0xFFCCFF33, 0xFFCCFF00, 0xFFCCCCFF, 0xFFCCCCCC,
        0xFFCCCC99, 0xFFCCCC66, 0xFFCCCC33, 0xFFCCCC00,
        0xFFCC99FF, 0xFFCC99CC, 0xFFCC9999, 0xFFCC9966,
        0xFFCC9933, 0xFFCC9900, 0xFFCC66FF, 0xFFCC66CC,
        0xFFCC6699, 0xFFCC6666, 0xFFCC6633, 0xFFCC6600,
        0xFFCC33FF, 0xFFCC33CC, 0xFFCC3399, 0xFFCC3366,
        0xFFCC3333, 0xFFCC3300, 0xFFCC00FF, 0xFFCC00CC,
        0xFFCC0099, 0xFFCC0066, 0xFFCC0033, 0xFFCC0000,
        0xFF99FFFF, 0xFF99FFCC, 0xFF99FF99, 0xFF99FF66,
        0xFF99FF33, 0xFF99FF00, 0xFF99CCFF, 0xFF99CCCC,
        0xFF99CC99, 0xFF99CC66, 0xFF99CC33, 0xFF99CC00,
        0xFF9999FF, 0xFF9999CC, 0xFF999999, 0xFF999966,
        0xFF999933, 0xFF999900, 0xFF9966FF, 0xFF9966CC,
        0xFF996699, 0xFF996666, 0xFF996633, 0xFF996600,
        0xFF9933FF, 0xFF9933CC, 0xFF993399, 0xFF993366,
        0xFF993333, 0xFF993300, 0xFF9900FF, 0xFF9900CC,
        0xFF990099, 0xFF990066, 0xFF990033, 0xFF990000,
        0xFF66FFFF, 0xFF66FFCC, 0xFF66FF99, 0xFF66FF66,
        0xFF66FF33, 0xFF66FF00, 0xFF66CCFF, 0xFF66CCCC,
        0xFF66CC99, 0xFF66CC66, 0xFF66CC33, 0xFF66CC00,
        0xFF6699FF, 0xFF6699CC, 0xFF669999, 0xFF669966,
        0xFF669933, 0xFF669900, 0xFF6666FF, 0xFF6666CC,
        0xFF666699, 0xFF666666, 0xFF666633, 0xFF666600,
        0xFF6633FF, 0xFF6633CC, 0xFF663399, 0xFF663366,
        0xFF663333, 0xFF663300, 0xFF6600FF, 0xFF6600CC,
        0xFF660099, 0xFF660066, 0xFF660033, 0xFF660000,
        0xFF33FFFF, 0xFF33FFCC, 0xFF33FF99, 0xFF33FF66,
        0xFF33FF33, 0xFF33FF00, 0xFF33CCFF, 0xFF33CCCC,
        0xFF33CC99, 0xFF33CC66, 0xFF33CC33, 0xFF33CC00,
        0xFF3399FF, 0xFF3399CC, 0xFF339999, 0xFF339966,
        0xFF339933, 0xFF339900, 0xFF3366FF, 0xFF3366CC,
        0xFF336699, 0xFF336666, 0xFF336633, 0xFF336600,
        0xFF3333FF, 0xFF3333CC, 0xFF333399, 0xFF333366,
        0xFF333333, 0xFF333300, 0xFF3300FF, 0xFF3300CC,
        0xFF330099, 0xFF330066, 0xFF330033, 0xFF330000,
        0xFF00FFFF, 0xFF00FFCC, 0xFF00FF99, 0xFF00FF66,
        0xFF00FF33, 0xFF00FF00, 0xFF00CCFF, 0xFF00CCCC,
        0xFF00CC99, 0xFF00CC66, 0xFF00CC33, 0xFF00CC00,
        0xFF0099FF, 0xFF0099CC, 0xFF009999, 0xFF009966,
        0xFF009933, 0xFF009900, 0xFF0066FF, 0xFF0066CC,
        0xFF006699, 0xFF006666, 0xFF006633, 0xFF006600,
        0xFF0033FF, 0xFF0033CC, 0xFF003399, 0xFF003366,
        0xFF003333, 0xFF003300, 0xFF0000FF, 0xFF0000CC,
        0xFF000099, 0xFF000066, 0xFF000033, 0xFFEE0000,
        0xFFDD0000, 0xFFBB0000, 0xFFAA0000, 0xFF880000,
        0xFF770000, 0xFF550000, 0xFF440000, 0xFF220000,
        0xFF110000, 0xFF00EE00, 0xFF00DD00, 0xFF00BB00,
        0xFF00AA00, 0xFF008800, 0xFF007700, 0xFF005500,
        0xFF004400, 0xFF002200, 0xFF001100, 0xFF0000EE,
        0xFF0000DD, 0xFF0000BB, 0xFF0000AA, 0xFF000088,
        0xFF000077, 0xFF000055, 0xFF000044, 0xFF000022,
        0xFF000011, 0xFFEEEEEE, 0xFFDDDDDD, 0xFFBBBBBB,
        0xFFAAAAAA, 0xFF888888, 0xFF777777, 0xFF555555,
        0xFF444444, 0xFF222222, 0xFF111111, 0xFF000000
    ];

    // http://www.libpng.org/pub/png/spec/1.2/PNG-Structure.html
    private static readonly byte[] PNG_SIGNATURE = [137, 80, 78, 71, 13, 10, 26, 10];

    private static IcnsImage TryDecodingPng(IcnsImageParser.IcnsElement element, IcnsType imageType)
    {
        if (element.data.Length < PNG_SIGNATURE.Length)
            return null; // definitely not a valid png

        for (int i = 0; i < PNG_SIGNATURE.Length; i++)
            if (element.data[i] != PNG_SIGNATURE[i])
                return null; // not a png

        using (var ms = new MemoryStream(element.data))
            return new IcnsImage((Bitmap)Image.FromStream(ms), imageType); // cast is valid, for PNG it will be Bitmap
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe void SetPixel(BitmapData data, int x, int y, uint color)
    {
        *(((uint*)data.Scan0) + y * data.Width + x) = color;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe uint GetPixel(BitmapData data, int x, int y)
    {
        return *(((uint*)data.Scan0) + y * data.Width + x);
    }

    private static void Decode1BPPImage(IcnsType imageType, byte[] imageData, BitmapData image)
    {
        int position = 0;
        int bitsLeft = 0;
        int value = 0;
        for (int y = 0; y < imageType.Height; y++)
            for (int x = 0; x < imageType.Width; x++)
            {
                if (bitsLeft == 0)
                {
                    value = 0xff & imageData[position++];
                    bitsLeft = 8;
                }

                uint argb;
                argb = (value & 0x80u) != 0 ? 0xff000000u : 0xffffffffu;
                value <<= 1;
                bitsLeft--;

                SetPixel(image, x, y, argb);
            }
    }

    private static void Decode4BPPImage(IcnsType imageType, byte[] imageData, BitmapData image)
    {
        int i = 0;
        bool visited = false;
        for (int y = 0; y < imageType.Height; y++)
            for (int x = 0; x < imageType.Width; x++)
            {
                int index;
                if (!visited)
                    index = 0xf & (imageData[i] >> 4);
                else
                    index = 0xf & imageData[i++];
                visited = !visited;

                SetPixel(image, x, y, PALETE_4BPP[index]);
            }
    }

    private static void Decode8BPPImage(IcnsType imageType, byte[] imageData, BitmapData image)
    {
        for (int y = 0; y < imageType.Height; y++)
            for (int x = 0; x < imageType.Width; x++)
            {
                int index = 0xff & imageData[y * imageType.Width + x];

                SetPixel(image, x, y, PALETTE_8BPP[index]);
            }
    }

    private static void Decode32BPPImage(IcnsType imageType, byte[] imageData, BitmapData image)
    {
        for (int y = 0; y < imageType.Height; y++)
            for (int x = 0; x < imageType.Width; x++)
            {
                uint argb = (0xff000000u /* the "alpha" is ignored */|
                             ((0xffu & imageData[4 * (y * imageType.Width + x) + 1]) << 16) |
                             ((0xffu & imageData[4 * (y * imageType.Width + x) + 2]) << 8) |
                             (0xffu & imageData[4 * (y * imageType.Width + x) + 3]));

                SetPixel(image, x, y, argb);
            }
    }

    private static void Decode32BPPImageARGB(IcnsType imageType, byte[] imageData, BitmapData image)
    {
        for (int y = 0; y < imageType.Height; y++)
            for (int x = 0; x < imageType.Width; x++)
            {
                uint argb = (((0xffu & imageData[4 * (y * imageType.Width + x) + 0]) << 24) |
                             ((0xffu & imageData[4 * (y * imageType.Width + x) + 1]) << 16) |
                             ((0xffu & imageData[4 * (y * imageType.Width + x) + 2]) << 8) |
                             (0xffu & imageData[4 * (y * imageType.Width + x) + 3]));

                SetPixel(image, x, y, argb);
            }
    }

    private static void Apply1BPPMask(byte[] maskData, BitmapData image)
    {
        int position;
        int bitsLeft = 0;
        int value = 0;

        // 1 bit icon types have image data followed by mask data in the same entry
        int totalBytes = (image.Width * image.Height + 7) / 8;

        if (maskData.Length >= 2 * totalBytes)
            position = totalBytes;
        else
            throw new ArgumentException("1 BPP mask underrun parsing ICNS file");

        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
            {
                if (bitsLeft == 0)
                {
                    value = 0xff & maskData[position++];
                    bitsLeft = 8;
                }

                uint alpha;
                alpha = (value & 0x80u) != 0 ? 0xffu : 0x00u;
                value <<= 1;
                bitsLeft--;

                SetPixel(image, x, y, (alpha << 24) | (0xffffffu & GetPixel(image, x, y)));
            }
    }

    private static void Apply8BPPMask(byte[] maskData, BitmapData image)
    {
        for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
            {
                uint alpha = 0xffu & maskData[y * image.Width + x];
                SetPixel(image, x, y, alpha << 24 | (0xffffffu & GetPixel(image, x, y)));
            }
    }

    private static IcnsImageParser.IcnsElement FindElement(IEnumerable<IcnsImageParser.IcnsElement> elements, int targetType)
    {
        foreach (IcnsImageParser.IcnsElement element in elements)
            if (element.type == targetType)
                return element;

        return null;
    }

    private static IcnsImage DecodeImage(IcnsImageParser.IcnsElement imageElement, IcnsImageParser.IcnsElement[] icnsElements)
    {
        IcnsType imageType = IcnsType.FindType(imageElement.type, IcnsType.TypeDetails.Mask);
        if (imageType == null)
            return null;

        IcnsType maskType = null;
        IcnsImageParser.IcnsElement maskElement = null;

        if (imageType.Details == IcnsType.TypeDetails.HasMask)
        {
            maskType = imageType;
            maskElement = imageElement;
        }
        else if (imageType.Details == IcnsType.TypeDetails.None)
        {
            maskType = IcnsType.FindType(imageType.Width, imageType.Height, 8, IcnsType.TypeDetails.Mask);
            if (maskType != null)
                maskElement = FindElement(icnsElements, maskType.Type);

            if (maskElement == null)
            {
                maskType = IcnsType.FindType(imageType.Width, imageType.Height, 1, IcnsType.TypeDetails.Mask);
                if (maskType != null)
                    maskElement = FindElement(icnsElements, maskType.Type);
            }
        }

        if (imageType.Details == IcnsType.TypeDetails.Compressed ||
            imageType.Details == IcnsType.TypeDetails.Retina)
        {
            IcnsImage result = TryDecodingPng(imageElement, imageType);
            if (result != null)
                return result; // png

            // Should try decoding using the j2k library

            return null; // couldn't be loaded
        }

        int expectedSize = (imageType.Width * imageType.Height * imageType.BitsPerPixel + 7) / 8;
        byte[] imageData;

        if (imageElement.data.Length < expectedSize)
        {
            if (imageType.BitsPerPixel == 32)
                imageData = Rle24Compression.Decompress(imageType.Width, imageType.Height, imageElement.data);
            else
                throw new Exception("Short image data but not a 32 bit compressed type");
        }
        else
            imageData = imageElement.data;

        Bitmap image = new(imageType.Width, imageType.Height, PixelFormat.Format32bppArgb);
        BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        switch (imageType.BitsPerPixel)
        {
            case 1:
                Decode1BPPImage(imageType, imageData, bitmapData);
                break;

            case 4:
                Decode4BPPImage(imageType, imageData, bitmapData);
                break;

            case 8:
                Decode8BPPImage(imageType, imageData, bitmapData);
                break;

            case 32:
                if (imageType.Details == IcnsType.TypeDetails.ARGB)
                    Decode32BPPImageARGB(imageType, imageData, bitmapData);
                else
                    Decode32BPPImage(imageType, imageData, bitmapData);
                break;

            default:
                image.UnlockBits(bitmapData);
                image.Dispose();
                throw new NotSupportedException("Unsupported bit depth " + imageType.BitsPerPixel);
        }

        if (maskElement != null)
        {
            switch (maskType.BitsPerPixel)
            {
                case 1:
                    Apply1BPPMask(maskElement.data, bitmapData);
                    break;

                case 8:
                    Apply8BPPMask(maskElement.data, bitmapData);
                    break;

                default:
                    image.UnlockBits(bitmapData);
                    image.Dispose();
                    throw new NotSupportedException("Unsupport mask bit depth " + maskType.BitsPerPixel);
            }
        }

        image.UnlockBits(bitmapData);
        return new IcnsImage(image, imageType);
    }

    public static IcnsImage[] DecodeAllImages(IcnsImageParser.IcnsElement[] icnsElements)
    {
        List<IcnsImage> result = [];

        for (int i = 0; i < icnsElements.Length; i++)
        {
            IcnsImage image = DecodeImage(icnsElements[i], icnsElements);
            if (image != null)
                result.Add(image);
        }
        return [.. result];
    }
}

internal class IcnsImage(Bitmap bitmap, IcnsType type)
{
    public Bitmap Bitmap => bitmap;

    public IcnsType Type => type;
}

internal static class IcnsImageParser
{
    public static int ICNS_MAGIC = IcnsType.TypeAsInt("icns");

    private static int Read4Bytes(Stream stream)
    {
        byte byte0 = (byte)stream.ReadByte();
        byte byte1 = (byte)stream.ReadByte();
        byte byte2 = (byte)stream.ReadByte();
        byte byte3 = (byte)stream.ReadByte();

        return ((0xff & byte0) << 24) |
               ((0xff & byte1) << 16) |
               ((0xff & byte2) << 8) |
               ((0xff & byte3) << 0);
    }

    private static void Write4Bytes(Stream stream, int value)
    {
        stream.WriteByte((byte)((value & 0xff000000) >> 24));
        stream.WriteByte((byte)((value & 0x00ff0000) >> 16));
        stream.WriteByte((byte)((value & 0x0000ff00) >> 8));
        stream.WriteByte((byte)((value & 0x000000ff) >> 0));
    }

    private class IcnsHeader(int magic, int fileSize)
    {
        public int magic = magic; // Magic literal (4 bytes), always "icns"
        public int fileSize = fileSize; // Length of file (4 bytes), in bytes.
    }

    private static IcnsHeader ReadIcnsHeader(Stream stream)
    {
        int Magic = Read4Bytes(stream);
        int FileSize = Read4Bytes(stream);

        if (Magic != ICNS_MAGIC)
            throw new Exception("Wrong ICNS magic");

        return new IcnsHeader(Magic, FileSize);
    }

    public class IcnsElement(int type, int elementSize, byte[] data)
    {
        public int type = type;
        public int elementSize = elementSize;
        public byte[] data = data;
    }

    private static IcnsElement ReadIcnsElement(Stream stream)
    {
        int type = Read4Bytes(stream); // Icon type (4 bytes)
        int elementSize = Read4Bytes(stream); // Length of data (4 bytes), in bytes, including this header
        byte[] data = new byte[elementSize - 8];
        stream.Read(data, 0, data.Length);

        return new IcnsElement(type, elementSize, data);
    }

    private static IcnsElement[] ReadImage(Stream stream)
    {
        IcnsHeader icnsHeader = ReadIcnsHeader(stream);

        List<IcnsElement> icnsElementList = [];
        for (int remainingSize = icnsHeader.fileSize - 8; remainingSize > 0;)
        {
            IcnsElement icnsElement = ReadIcnsElement(stream);
            icnsElementList.Add(icnsElement);
            remainingSize -= icnsElement.elementSize;
        }

        IcnsElement[] icnsElements = new IcnsElement[icnsElementList.Count];
        for (int i = 0; i < icnsElements.Length; i++)
            icnsElements[i] = icnsElementList[i];

        return icnsElements;
    }

    public static IcnsImage GetImage(string filename)
    {
        using (var stream = new FileStream(filename, FileMode.Open))
            return GetImage(stream);
    }

    public static IcnsImage GetImage(Stream stream)
    {
        IcnsElement[] icnsContents = ReadImage(stream);
        IcnsImage[] result = IcnsDecoder.DecodeAllImages(icnsContents);
        if (result.Length <= 0)
            throw new NotSupportedException("No icons in ICNS file");

        IcnsImage max = null;
        foreach (IcnsImage bitmap in result)
            if (bitmap.Bitmap != null && (max == null || (bitmap.Bitmap.Width > bitmap.Bitmap.Height)))
                max = bitmap;

        return max;
    }

    public static IcnsImage[] GetImages(string filename)
    {
        using (var stream = new FileStream(filename, FileMode.Open))
            return GetImages(stream);
    }

    public static IcnsImage[] GetImages(Stream stream)
    {
        IcnsElement[] icnsContents = ReadImage(stream);
        return IcnsDecoder.DecodeAllImages(icnsContents);
    }

    public static void WriteImage(Bitmap src, Stream stream)
    {
        IcnsType imageType = IcnsType.FindType(src.Width, src.Height, 32, IcnsType.TypeDetails.None);
        if (imageType == null)
            throw new NotSupportedException($"Invalid/unsupported source: {src.Width}x{src.Height}");

        Write4Bytes(stream, ICNS_MAGIC);
        Write4Bytes(stream, 4 + 4 + 4 + 4 + 4 * imageType.Width * imageType.Height + 4 + 4 + imageType.Width * imageType.Height);

        Write4Bytes(stream, imageType.Type);
        Write4Bytes(stream, 4 + 4 + 4 * imageType.Width * imageType.Height);

        BitmapData bitmapData = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        // the image
        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                uint argb = IcnsDecoder.GetPixel(bitmapData, x, y);
                stream.WriteByte(0);
                stream.WriteByte((byte)((argb & 0x00ff0000) >> 16));
                stream.WriteByte((byte)((argb & 0x0000ff00) >> 8));
                stream.WriteByte((byte)((argb & 0x000000ff) >> 0));
            }
        }

        // mask
        IcnsType maskType = IcnsType.FindType(src.Width, src.Height, 8, IcnsType.TypeDetails.Mask);
        Write4Bytes(stream, maskType.Type);
        Write4Bytes(stream, 4 + 4 + imageType.Width * imageType.Width);

        for (int y = 0; y < src.Height; y++)
        {
            for (int x = 0; x < src.Width; x++)
            {
                uint argb = IcnsDecoder.GetPixel(bitmapData, x, y);
                stream.WriteByte((byte)((argb & 0xff000000) >> 24));
            }
        }
    }

    public static void WriteImage(Bitmap src, string filename)
    {
        using (var stream = new FileStream(filename, FileMode.Create))
            WriteImage(src, stream);
    }
}

internal class IcnsType
{
    private readonly int type;
    private readonly int width;
    private readonly int height;
    private readonly int bitsPerPixel;
    private readonly TypeDetails details;

    // https://en.wikipedia.org/wiki/Apple_Icon_Image_format
    public static readonly IcnsType[] ALL_TYPES =
    [
      // 16x12
      new IcnsType("icm#", 16, 12, 1, TypeDetails.HasMask),
      new IcnsType("icm4", 16, 12, 4, TypeDetails.None),
      new IcnsType("icm8", 16, 12, 8, TypeDetails.None),
      // 16x16
      new IcnsType("ics#", 16, 16, 1, TypeDetails.Mask),
      new IcnsType("ics4", 16, 16, 4, TypeDetails.None),
      new IcnsType("ics8", 16, 16, 8, TypeDetails.None),
      new IcnsType("is32", 16, 16, 32, TypeDetails.None),
      new IcnsType("s8mk", 16, 16, 8, TypeDetails.Mask),
      new IcnsType("icp4", 16, 16, 32, TypeDetails.Compressed),
      new IcnsType("ic04", 16, 16, 32, TypeDetails.ARGB),
      // 18x18
      new IcnsType("icsb", 18, 18, 32, TypeDetails.ARGB), // not tested
      // 32x32
      new IcnsType("ICON", 32, 32, 1, TypeDetails.None),
      new IcnsType("ICN#", 32, 32, 1, TypeDetails.HasMask),
      new IcnsType("icl4", 32, 32, 4, TypeDetails.None),
      new IcnsType("icl8", 32, 32, 8, TypeDetails.None),
      new IcnsType("il32", 32, 32, 32, TypeDetails.None),
      new IcnsType("l8mk", 32, 32, 8, TypeDetails.Mask),
      new IcnsType("icp5", 32, 32, 32, TypeDetails.Compressed),
      new IcnsType("ic11", 32, 32, 32, TypeDetails.Retina),
      new IcnsType("ic05", 32, 32, 32, TypeDetails.ARGB),
      // 36x36
      new IcnsType("icsB", 36, 36, 32, TypeDetails.ARGB), // not tested
      // 48x48
      new IcnsType("ich#", 48, 48, 1, TypeDetails.Mask),
      new IcnsType("ich4", 48, 48, 4, TypeDetails.None),
      new IcnsType("ich8", 48, 48, 8, TypeDetails.None),
      new IcnsType("ih32", 48, 48, 32, TypeDetails.None),
      new IcnsType("h8mk", 48, 48, 8, TypeDetails.Mask),
      // 64x64
      new IcnsType("icp6", 64, 64, 32, TypeDetails.Compressed),
      new IcnsType("ic12", 64, 64, 32, TypeDetails.Retina),
      // 128x128
      new IcnsType("it32", 128, 128, 32, TypeDetails.None),
      new IcnsType("t8mk", 128, 128, 8, TypeDetails.Mask),
      new IcnsType("ic07", 128, 128, 32, TypeDetails.Compressed),
      // other
      new IcnsType("ic08", 256, 256, 32, TypeDetails.Compressed),
      new IcnsType("ic13", 256, 256, 32, TypeDetails.Retina),
      new IcnsType("ic09", 512, 512, 32, TypeDetails.Compressed),
      new IcnsType("ic14", 512, 512, 32, TypeDetails.Retina),
      new IcnsType("ic10", 1024, 1024, 32, TypeDetails.Retina),
    ];

    private IcnsType(string type, int width, int height, int bitsPerPixel, TypeDetails details)
    {
        this.type = TypeAsInt(type);
        this.width = width;
        this.height = height;
        this.bitsPerPixel = bitsPerPixel;
        this.details = details;
    }

    public int Type => type;

    public int Width => width;

    public int Height => height;

    public int BitsPerPixel => bitsPerPixel;

    public TypeDetails Details => details;

    public static IcnsType FindType(int type, TypeDetails ignoreDetails)
    {
        for (int i = 0; i < ALL_TYPES.Length; i++)
        {
            if (ALL_TYPES[i].type != type)
                continue;

            if (ignoreDetails != 0 && ALL_TYPES[i].Details == ignoreDetails)
                continue;

            return ALL_TYPES[i];
        }
        return null;
    }

    public static IcnsType FindType(int width, int height, int bpp, TypeDetails details)
    {
        for (int i = 0; i < ALL_TYPES.Length; i++)
        {
            IcnsType type = ALL_TYPES[i];
            if (type.width == width &&
                type.height == height &&
                type.bitsPerPixel == bpp &&
                type.details == details)
                return type;
        }

        return null;
    }

    public static int TypeAsInt(string type)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(type);

        if (bytes.Length != 4)
            throw new Exception("Invalid ICNS type");

        return ((0xff & bytes[0]) << 24) |
               ((0xff & bytes[1]) << 16) |
               ((0xff & bytes[2]) << 8) |
               (0xff & bytes[3]);
    }

    public static string DescribeType(int type)
    {
        byte[] bytes =
        [
            (byte)(0xff & (type >> 24)),
            (byte)(0xff & (type >> 16)),
            (byte)(0xff & (type >> 8)),
            (byte)(0xff & type),
        ];
        return Encoding.ASCII.GetString(bytes);
    }

    public enum TypeDetails
    {
        /// <summary>
        /// The default image with no detils.
        /// </summary>
        None,

        /// <summary>
        /// The image is alpha mask.
        /// </summary>
        Mask,

        /// <summary>
        /// Has alpha mask.
        /// </summary>
        HasMask,

        /// <summary>
        /// Whole 4 channels are used.
        /// </summary>
        ARGB,

        /// <summary>
        /// Compressed, j2k or PNG codec is used.
        /// </summary>
        Compressed,

        /// <summary>
        /// Retina (2x) image. j2k or PNG is used.
        /// </summary>
        Retina,
    }
}

internal class Rle24Compression
{
    public static byte[] Decompress(int width, int height, byte[] data)
    {
        int pixelCount = width * height;
        byte[] result = new byte[4 * pixelCount];

        // Several ICNS parsers advance by 4 bytes here:
        // http://code.google.com/p/icns2png/ - when the width is >= 128
        // http://icns.sourceforge.net/ - when those 4 bytes are all zero
        //
        // A scan of all .icns files on MacOS shows that
        // all 128x128 images indeed start with 4 zeroes,
        // while all smaller images don't.
        // However it is dangerous to assume
        // that 4 initial zeroes always need to be skipped,
        // because they could encode valid pixels on smaller images.
        // So always skip on 128x128, and never skip on anything else.
        int dataPos = 0;
        if (width >= 128 && height >= 128)
            dataPos = 4;

        // argb, band by band in 3 passes, with no alpha
        for (int band = 1; band <= 3; band++)
        {
            int remaining = pixelCount;
            int resultPos = 0;
            while (remaining > 0)
            {
                if ((data[dataPos] & 0x80) != 0)
                {
                    int count = (0xff & data[dataPos]) - 125;
                    for (int i = 0; i < count; i++)
                    {
                        int idx = band + 4 * (resultPos++);
                        if (idx < result.Length)
                            result[idx] = data[dataPos + 1];
                    }
                    dataPos += 2;
                    remaining -= count;
                }
                else
                {
                    int count = (0xff & data[dataPos]) + 1;
                    dataPos++;
                    for (int i = 0; i < count; i++)
                    {
                        byte value = data[dataPos++];
                        int idx = band + 4 * (resultPos++);
                        if (idx < result.Length)
                            result[idx] = value;
                    }
                    remaining -= count;
                }
            }
        }
        return result;
    }
}
