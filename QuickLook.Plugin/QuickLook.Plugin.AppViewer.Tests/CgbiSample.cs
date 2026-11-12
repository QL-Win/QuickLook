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

namespace QuickLook.Plugin.AppViewer.Tests;

internal static class CgbiSample
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static byte[] Create(int width, int height, byte blue, byte green, byte red, byte alpha)
    {
        int rowBytes = width * 4;
        byte[] raw = new byte[(rowBytes + 1) * height];

        for (int y = 0; y < height; y++)
        {
            int row = y * (rowBytes + 1) + 1;
            for (int x = 0; x < width; x++)
            {
                int pixel = row + x * 4;
                raw[pixel] = blue;
                raw[pixel + 1] = green;
                raw[pixel + 2] = red;
                raw[pixel + 3] = alpha;
            }
        }

        byte[] compressed;
        using (var buffer = new MemoryStream())
        {
            using (DeflaterOutputStream deflater = new(buffer, new Deflater(Deflater.BEST_COMPRESSION, true))
            {
                IsStreamOwner = false,
            })
            {
                deflater.Write(raw, 0, raw.Length);
            }

            compressed = buffer.ToArray();
        }

        using var output = new MemoryStream();
        output.Write(Signature, 0, Signature.Length);

        WriteChunk(output, "CgBI", []);

        byte[] header = new byte[13];
        WriteInt32BigEndian(header, 0, width);
        WriteInt32BigEndian(header, 4, height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(output, "IHDR", header);

        WriteChunk(output, "IDAT", compressed);
        WriteChunk(output, "IEND", []);

        return output.ToArray();
    }

    public static void WriteChunk(Stream stream, string type, byte[] data)
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
}
