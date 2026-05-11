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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace QuickLook.Plugin.ArchiveViewer.CompoundFileBinary;

/// <summary>
/// Extracts thumbnail images from Windows Thumbs.db files (OLE Compound File Binary).
///
/// Thumbs.db thumbnail streams are NOT raw JPEG/PNG. They carry a private header:
///
///   [0..3]  header_offset  (uint32 LE) – byte offset at which the real image data begins
///   [4..]   metadata       – version/hash info; exact length = header_offset - 4
///   [header_offset..]  image payload
///
/// The image payload itself can be one of three formats:
///   1. Standard JPEG   – starts with FF D8
///   2. Standard PNG    – starts with 89 50 4E 47 0D 0A 1A 0A
///   3. Raw DCT stream  – starts with 01 00 00 00 (second_header == 1).
///      These are bare DCT bitstreams without a JFIF envelope.  They must be
///      reconstructed by prepending hard-coded JFIF / quantization / Huffman
///      tables, exactly as done by the open-source Thumbs Viewer by Eric Kutcher
///      (https://thumbsviewer.github.io/).
///
/// This class intentionally does NOT touch the generic <see cref="CompoundFileExtractor"/>
/// which handles .cfb / .eif files.
/// </summary>
public static class ThumbsDbExtractor
{
    // -- JPEG reconstruction constants (from Thumbs Viewer by Eric Kutcher) ------

    // Standard JFIF APP0 marker segment (20 bytes)
    private static readonly byte[] JfifHeader =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46,
        0x00, 0x01, 0x01, 0x01, 0x00, 0x60, 0x00, 0x60, 0x00, 0x00,
    ];

    // Luminance + Chrominance quantization tables DQT segment (138 bytes)
    private static readonly byte[] Quantization =
    [
        0xFF, 0xDB, 0x00, 0x43, 0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09,
        0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12, 0x13, 0x0F, 0x14, 0x1D,
        0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20, 0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C,
        0x1C, 0x28, 0x37, 0x29, 0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32,
        0x3C, 0x2E, 0x33, 0x34, 0x32,
        0xFF, 0xDB, 0x00, 0x43, 0x01, 0x09, 0x09, 0x09, 0x0C, 0x0B, 0x0C, 0x18, 0x0D, 0x0D, 0x18, 0x32,
        0x21, 0x1C, 0x21, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32,
        0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32,
        0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32, 0x32,
        0x32, 0x32, 0x32, 0x32, 0x32,
    ];

    // Standard Huffman tables DHT segment (216 bytes)
    private static readonly byte[] HuffmanTable =
    [
        0xFF, 0xC4, 0x00, 0x1F, 0x00, 0x00, 0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A,
        0x0B, 0xFF, 0xC4, 0x00, 0xB5, 0x10, 0x00, 0x02, 0x01, 0x03, 0x03, 0x02, 0x04, 0x03, 0x05, 0x05,
        0x04, 0x04, 0x00, 0x00, 0x01, 0x7D, 0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31,
        0x41, 0x06, 0x13, 0x51, 0x61, 0x07, 0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08, 0x23, 0x42,
        0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0, 0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16, 0x17, 0x18,
        0x19, 0x1A, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43,
        0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x63,
        0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x83,
        0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A,
        0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8,
        0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6,
        0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF1, 0xF2,
        0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA,
    ];

    // -- Public API ---------------------------------------------------------------

    /// <summary>
    /// Extracts all thumbnail images from a Thumbs.db file into
    /// <paramref name="destinationDirectory"/>, stripping the private Thumbs.db header
    /// and reconstructing bare-DCT JPEG streams where necessary.
    /// </summary>
    public static void ExtractToDirectory(string thumbsDbPath, string destinationDirectory)
    {
        if (!Directory.Exists(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        if (!File.Exists(thumbsDbPath))
            throw new FileNotFoundException("Thumbs.db not found.", thumbsDbPath);

        using DisposableIStorage storage = new(thumbsDbPath, STGM.READ | STGM.SHARE_DENY_WRITE, IntPtr.Zero);
        IEnumerator<STATSTG> enumerator = storage.EnumElements();

        while (enumerator.MoveNext())
        {
            STATSTG stat = enumerator.Current;

            // Only process stream entries; skip storages and the Catalog metadata stream.
            if (stat.type != (int)STGTY.STGTY_STREAM)
                continue;
            if (stat.pwcsName.Equals("Catalog", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                byte[] raw = ReadStream(storage, stat);
                (byte[] imageBytes, string ext) = StripHeaderAndDetect(raw);
                if (imageBytes == null) continue;

                string outPath = Path.Combine(destinationDirectory, stat.pwcsName + ext);
                File.WriteAllBytes(outPath, imageBytes);
            }
            catch
            {
                // Skip unreadable or malformed streams
            }
        }
    }

    // -- Internals ----------------------------------------------------------------

    /// <summary>
    /// Reads the full content of a stream entry from an open IStorage.
    /// </summary>
    private static byte[] ReadStream(DisposableIStorage storage, STATSTG stat)
    {
        using DisposableIStream stream = storage.OpenStream(stat.pwcsName, IntPtr.Zero, STGM.READ | STGM.SHARE_EXCLUSIVE);
        STATSTG streamStat = stream.Stat((int)STATFLAG.STATFLAG_DEFAULT);
        byte[] buf = new byte[streamStat.cbSize];
        stream.Read(buf, buf.Length);
        return buf;
    }

    /// <summary>
    /// Strips the private Thumbs.db header from <paramref name="raw"/> and returns the
    /// image bytes plus the appropriate file extension.
    ///
    /// Returns <c>(null, null)</c> when the data is too short or the format is unrecognised.
    /// </summary>
    private static (byte[] imageBytes, string ext) StripHeaderAndDetect(byte[] raw)
    {
        if (raw == null || raw.Length < 8)
            return (null, null);

        // First 4 bytes (LE uint32) = offset at which the real image payload begins.
        uint headerOffset = BitConverter.ToUInt32(raw, 0);
        if (headerOffset >= (uint)raw.Length)
            headerOffset = 0;   // fallback: treat the whole buffer as the payload

        int offset = (int)headerOffset;
        int remaining = raw.Length - offset;

        if (remaining < 2)
            return (null, null);

        // -- Case 1: standard JPEG --
        if (remaining >= 2 && raw[offset] == 0xFF && raw[offset + 1] == 0xD8)
        {
            byte[] img = new byte[remaining];
            Buffer.BlockCopy(raw, offset, img, 0, remaining);
            return (img, ".jpg");
        }

        // -- Case 2: standard PNG --
        if (remaining >= 8
            && raw[offset] == 0x89 && raw[offset + 1] == 0x50   // \x89P
            && raw[offset + 2] == 0x4E && raw[offset + 3] == 0x47  // NG
            && raw[offset + 4] == 0x0D && raw[offset + 5] == 0x0A  // \r\n
            && raw[offset + 6] == 0x1A && raw[offset + 7] == 0x0A) // \x1A\n
        {
            byte[] img = new byte[remaining];
            Buffer.BlockCopy(raw, offset, img, 0, remaining);
            return (img, ".png");
        }

        // -- Case 3: raw DCT (second_header == 1) --
        // The image payload begins at raw[offset] and is a 32-bit LE value of 1.
        // The actual bitstream starts at absolute position 52 in raw[],
        // with a 22-byte SOF block at absolute positions 30–51.
        // Reconstruct a valid JPEG by prepending JFIF / DQT / SOF0 / DHT tables.
        if (remaining >= 4
            && raw[offset] == 0x01 && raw[offset + 1] == 0x00
            && raw[offset + 2] == 0x00 && raw[offset + 3] == 0x00
            && raw.Length > 52)
        {
            // Layout of reconstructed JPEG:
            //   JFIF header    20 bytes  offset   0
            //   Quantization  138 bytes  offset  20
            //   SOF block      22 bytes  offset 158   ← taken from raw[30..51]
            //   Huffman table 216 bytes  offset 180
            //   DCT bitstream  ?  bytes  offset 396   ← taken from raw[52..]
            int dctLength = raw.Length - 52;
            byte[] img = new byte[20 + 138 + 22 + 216 + dctLength];

            Buffer.BlockCopy(JfifHeader, 0, img, 0, 20);
            Buffer.BlockCopy(Quantization, 0, img, 20, 138);
            Buffer.BlockCopy(raw, 30, img, 158, 22);
            Buffer.BlockCopy(HuffmanTable, 0, img, 180, 216);
            Buffer.BlockCopy(raw, 52, img, 396, dctLength);

            return (img, ".jpg");
        }

        return (null, null);
    }
}
