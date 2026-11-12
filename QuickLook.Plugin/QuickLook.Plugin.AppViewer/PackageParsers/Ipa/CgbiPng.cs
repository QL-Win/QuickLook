// Copyright © 2017-2026 QL-Win Contributors
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

using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.IO;
using System.Text;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Ipa;

/// <summary>
/// Converts Apple's CgBI PNG variant into a standard PNG.
/// CgBI files store premultiplied BGRA pixels and compress the image data as raw deflate,
/// which regular PNG decoders interpret as ABGR and therefore render with swapped colors.
/// </summary>
public static class CgbiPng
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool IsCgbi(byte[] data)
    {
        if (data == null || data.Length < 20 || !HasSignature(data))
        {
            return false;
        }

        return Encoding.ASCII.GetString(data, 12, 4) == "CgBI";
    }

    public static byte[] ConvertToStandardPng(byte[] data)
    {
        if (!IsCgbi(data))
        {
            return data;
        }

        int width = 0;
        int height = 0;
        int bitDepth = 0;
        int colorType = -1;
        int interlace = 0;
        using var idat = new MemoryStream();

        int position = 8;
        while (position + 12 <= data.Length)
        {
            int length = ReadInt32BigEndian(data, position);
            if (length < 0 || (long)position + 12 + length > data.Length)
            {
                return data;
            }

            string type = Encoding.ASCII.GetString(data, position + 4, 4);
            int dataStart = position + 8;

            if (type == "IEND")
            {
                break;
            }

            if (type == "IHDR")
            {
                if (length < 13)
                {
                    return data;
                }

                width = ReadInt32BigEndian(data, dataStart);
                height = ReadInt32BigEndian(data, dataStart + 4);
                bitDepth = data[dataStart + 8];
                colorType = data[dataStart + 9];
                interlace = data[dataStart + 12];
            }
            else if (type == "IDAT")
            {
                idat.Write(data, dataStart, length);
            }

            position = dataStart + length + 4;
        }

        // Only the 8-bit non-interlaced RGB/RGBA layouts produced by Apple's pngcrush are handled.
        if (width <= 0 || height <= 0 || bitDepth != 8 || (colorType != 2 && colorType != 6) || interlace != 0)
        {
            return data;
        }

        int bytesPerPixel = colorType == 6 ? 4 : 3;
        long rowBytesLong = (long)width * bytesPerPixel;
        long filteredLength = (rowBytesLong + 1) * height;
        if (rowBytesLong > int.MaxValue || filteredLength > int.MaxValue)
        {
            return data;
        }

        int rowBytes = (int)rowBytesLong;
        byte[] filtered = new byte[(int)filteredLength];

        try
        {
            Inflater inflater = new(true);
            byte[] compressed = idat.ToArray();
            inflater.SetInput(compressed);

            int totalRead = 0;
            while (totalRead < filtered.Length)
            {
                int read = inflater.Inflate(filtered, totalRead, filtered.Length - totalRead);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            if (totalRead < filtered.Length)
            {
                return data;
            }
        }
        catch (Exception)
        {
            return data;
        }

        byte[] pixels = Unfilter(filtered, width, height, bytesPerPixel);
        if (pixels == null)
        {
            return data;
        }

        byte[] rgba = SwapChannels(pixels, bytesPerPixel);
        return EncodePng(rgba, width, height, colorType, rowBytes);
    }

    private static byte[] Unfilter(byte[] filtered, int width, int height, int bytesPerPixel)
    {
        int rowBytes = width * bytesPerPixel;
        byte[] pixels = new byte[rowBytes * height];

        for (int y = 0; y < height; y++)
        {
            int source = y * (rowBytes + 1);
            int filter = filtered[source];
            int destination = y * rowBytes;
            int previous = destination - rowBytes;
            source++;

            for (int x = 0; x < rowBytes; x++)
            {
                int raw = filtered[source + x];
                int left = x >= bytesPerPixel ? pixels[destination + x - bytesPerPixel] : 0;
                int up = y > 0 ? pixels[previous + x] : 0;
                int upLeft = y > 0 && x >= bytesPerPixel ? pixels[previous + x - bytesPerPixel] : 0;

                int value = filter switch
                {
                    0 => raw,
                    1 => raw + left,
                    2 => raw + up,
                    3 => raw + ((left + up) >> 1),
                    4 => raw + Paeth(left, up, upLeft),
                    _ => -1,
                };

                if (value < 0)
                {
                    return null;
                }

                pixels[destination + x] = (byte)value;
            }
        }

        return pixels;
    }

    private static byte[] SwapChannels(byte[] pixels, int bytesPerPixel)
    {
        byte[] rgba = new byte[pixels.Length];

        for (int i = 0; i < pixels.Length; i += bytesPerPixel)
        {
            int blue = pixels[i];
            int green = pixels[i + 1];
            int red = pixels[i + 2];

            if (bytesPerPixel == 4)
            {
                int alpha = pixels[i + 3];
                if (alpha > 0 && alpha < 255)
                {
                    red = Math.Min(255, (red * 255 + alpha / 2) / alpha);
                    green = Math.Min(255, (green * 255 + alpha / 2) / alpha);
                    blue = Math.Min(255, (blue * 255 + alpha / 2) / alpha);
                }

                rgba[i] = (byte)red;
                rgba[i + 1] = (byte)green;
                rgba[i + 2] = (byte)blue;
                rgba[i + 3] = (byte)alpha;
            }
            else
            {
                rgba[i] = (byte)red;
                rgba[i + 1] = (byte)green;
                rgba[i + 2] = (byte)blue;
            }
        }

        return rgba;
    }

    private static byte[] EncodePng(byte[] pixels, int width, int height, int colorType, int rowBytes)
    {
        using var output = new MemoryStream();
        output.Write(Signature, 0, Signature.Length);

        byte[] header = new byte[13];
        WriteInt32BigEndian(header, 0, width);
        WriteInt32BigEndian(header, 4, height);
        header[8] = 8;
        header[9] = (byte)colorType;
        WriteChunk(output, "IHDR", header);

        using (var raw = new MemoryStream())
        {
            using (DeflaterOutputStream deflater = new(raw, new Deflater(Deflater.BEST_COMPRESSION))
            {
                IsStreamOwner = false,
            })
            {
                for (int y = 0; y < height; y++)
                {
                    deflater.WriteByte(0);
                    deflater.Write(pixels, y * rowBytes, rowBytes);
                }
            }

            WriteChunk(output, "IDAT", raw.ToArray());
        }

        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        WriteInt32BigEndian(stream, data.Length);
        stream.Write(typeBytes, 0, typeBytes.Length);
        stream.Write(data, 0, data.Length);

        Crc32 crc = new();
        crc.Update(typeBytes);
        if (data.Length > 0)
        {
            crc.Update(data);
        }

        WriteInt32BigEndian(stream, unchecked((int)crc.Value));
    }

    private static bool HasSignature(byte[] data)
    {
        for (int i = 0; i < Signature.Length; i++)
        {
            if (data[i] != Signature[i])
            {
                return false;
            }
        }

        return true;
    }

    private static int ReadInt32BigEndian(byte[] data, int offset)
    {
        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    private static void WriteInt32BigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static void WriteInt32BigEndian(Stream stream, int value)
    {
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    private static int Paeth(int left, int up, int upLeft)
    {
        int estimate = left + up - upLeft;
        int distanceLeft = Math.Abs(estimate - left);
        int distanceUp = Math.Abs(estimate - up);
        int distanceUpLeft = Math.Abs(estimate - upLeft);

        if (distanceLeft <= distanceUp && distanceLeft <= distanceUpLeft)
        {
            return left;
        }

        return distanceUp <= distanceUpLeft ? up : upLeft;
    }
}
