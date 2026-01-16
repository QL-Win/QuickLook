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
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace QuickLook.Plugin.ArchiveViewer.ChromiumResourcePackage;

/// <summary>
/// Provides static methods for extracting resources from Chrome .pak archive files.
/// </summary>
public static class PakExtractor
{
    /// <summary>
    /// Extracts all resources from a Chrome .pak file and saves them as individual files in the specified output directory.
    /// Each resource file is named using a 9-digit zero-padded decimal string representing its resource ID (e.g., "000000001").
    /// </summary>
    /// <param name="fileName">The path to the .pak file to extract.</param>
    /// <param name="outputDirectory">The directory where extracted resource files will be saved.</param>
    public static void ExtractToDirectory(string fileName, string outputDirectory)
    {
        using var stream = File.OpenRead(fileName);
        using var br = new BinaryReader(stream);
        var version = br.ReadUInt32();
        var encoding = br.ReadByte();
        stream.Seek(3, SeekOrigin.Current); // Skip 3 reserved bytes
        var resourceCount = br.ReadUInt16();
        var aliasCount = br.ReadUInt16();

        Entry[] entries = new Entry[resourceCount + 1];
        for (int i = 0; i < resourceCount + 1; i++)
        {
            var resourceId = br.ReadUInt16();
            var fileOffset = br.ReadUInt32();
            entries[i] = new Entry(resourceId, fileOffset);
        }
        // Aliases are not used in extraction, so just skip reading if not needed
        stream.Seek(aliasCount * 4, SeekOrigin.Current);

        Directory.CreateDirectory(outputDirectory);

        // Use a single buffer for all resources (max resource size)
        int maxLength = 0;
        for (int i = 0; i < resourceCount; i++)
        {
            int len = (int)(entries[i + 1].FileOffset - entries[i].FileOffset);
            if (len > maxLength) maxLength = len;
        }
        byte[] buffer = ArrayPool<byte>.Shared.Rent(maxLength);

        try
        {
            for (int i = 0; i < resourceCount; i++)
            {
                int length = (int)(entries[i + 1].FileOffset - entries[i].FileOffset);
                stream.Seek(entries[i].FileOffset, SeekOrigin.Begin);
                int read = 0;
                while (read < length)
                {
                    int n = stream.Read(buffer, read, length - read);
                    if (n == 0) break;
                    read += n;
                }
                string resourceName = entries[i].ResourceId.ToString("D9");
                using var file = File.Create(Path.Combine(outputDirectory, resourceName));
                file.Write(buffer, 0, length);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Extracts all resources from a Chrome .pak file and returns them as a dictionary.
    /// The dictionary key is a 9-digit zero-padded decimal string representing the resource ID,
    /// optionally with a guessed file extension, and the value is the resource content as a byte array.
    /// </summary>
    /// <param name="fileName">The path to the .pak file to extract.</param>
    /// <param name="appendExtension">If true, append guessed file extension to the key (e.g., "000000001.png").</param>
    /// <returns>A dictionary mapping resource names to their byte content.</returns>
    public static Dictionary<string, byte[]> ExtractToDictionary(string fileName, bool appendExtension = true)
    {
        using var stream = File.OpenRead(fileName);
        using var br = new BinaryReader(stream);
        var version = br.ReadUInt32();
        var encoding = br.ReadByte();
        stream.Seek(3, SeekOrigin.Current); // Skip 3 reserved bytes
        var resourceCount = br.ReadUInt16();
        var aliasCount = br.ReadUInt16();

        Entry[] entries = new Entry[resourceCount + 1];
        for (int i = 0; i < resourceCount + 1; i++)
        {
            var resourceId = br.ReadUInt16();
            var fileOffset = br.ReadUInt32();
            entries[i] = new Entry(resourceId, fileOffset);
        }
        // Aliases are not used in extraction, so just skip reading if not needed
        stream.Seek(aliasCount * 4, SeekOrigin.Current);

        // Use a single buffer for all resources (max resource size)
        int maxLength = 0;
        for (int i = 0; i < resourceCount; i++)
        {
            int len = (int)(entries[i + 1].FileOffset - entries[i].FileOffset);
            if (len > maxLength) maxLength = len;
        }
        byte[] buffer = ArrayPool<byte>.Shared.Rent(maxLength);
        var dict = new Dictionary<string, byte[]>(resourceCount);

        try
        {
            for (int i = 0; i < resourceCount; i++)
            {
                int length = (int)(entries[i + 1].FileOffset - entries[i].FileOffset);
                stream.Seek(entries[i].FileOffset, SeekOrigin.Begin);
                int read = 0;
                while (read < length)
                {
                    int n = stream.Read(buffer, read, length - read);
                    if (n == 0) break;
                    read += n;
                }
                string resourceName = entries[i].ResourceId.ToString("D9");
                if (appendExtension)
                {
                    string ext = GuessFileExtension(buffer, length);
                    resourceName += ext;
                }
                // Copy only the valid part of buffer
                var data = new byte[length];
                Buffer.BlockCopy(buffer, 0, data, 0, length);
                dict[resourceName] = data;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
        return dict;
    }

    /// <summary>
    /// Guesses the file extension based on the content of the given byte array.
    /// Returns common extensions such as "png", "jpg", "gif", "bmp", "pdf", "zip", etc.
    /// Returns ".bin" if the type cannot be determined.
    /// </summary>
    /// <param name="data">The byte array containing the file data.</param>
    /// <param name="length">The valid length of data to check (for pooled buffer usage).</param>
    /// <returns>The guessed file extension with dot, e.g. ".png".</returns>
    public static string GuessFileExtension(byte[] data, int length = -1)
    {
        if (data == null || (length < 0 ? data.Length : length) < 4)
            return ".bin";
        int len = length < 0 ? data.Length : length;
        // PNG
        if (len > 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            return ".png";
        // JPEG
        if (data[0] == 0xFF && data[1] == 0xD8)
            return ".jpg";
        // GIF
        if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
            return ".gif";
        // BMP
        if (data[0] == 0x42 && data[1] == 0x4D)
            return ".bmp";
        // PDF
        if (data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46)
            return ".pdf";
        // ZIP/Office
        if (data[0] == 0x50 && data[1] == 0x4B && (data[2] == 0x03 || data[2] == 0x05 || data[2] == 0x07) && (data[3] == 0x04 || data[3] == 0x06 || data[3] == 0x08))
            return ".zip";
        // RAR
        if (len > 7 && data[0] == 0x52 && data[1] == 0x61 && data[2] == 0x72 && data[3] == 0x21 && data[4] == 0x1A && data[5] == 0x07 && (data[6] == 0x00 || data[6] == 0x01))
            return ".rar";
        // 7z
        if (len > 5 && data[0] == 0x37 && data[1] == 0x7A && data[2] == 0xBC && data[3] == 0xAF && data[4] == 0x27 && data[5] == 0x1C)
            return ".7z";
        // MP3
        if (len > 2 && data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33)
            return ".mp3";
        // MP4
        if (len > 11 && data[4] == 0x66 && data[5] == 0x74 && data[6] == 0x79 && data[7] == 0x70)
            return ".mp4";
        // WebP
        if (len > 11 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            return ".webp";
        // TXT (heuristic: printable ASCII, no nulls)
        bool isText = true;
        for (int i = 0; i < Math.Min(64, len); i++)
        {
            if (data[i] == 0 || (data[i] < 0x09) || (data[i] > 0x0D && data[i] < 0x20))
            {
                isText = false;
                break;
            }
        }
        if (isText)
            return ".txt";
        return ".bin";
    }
}

/// <summary>
/// Represents a resource entry in the .pak file, containing the resource ID and its file offset.
/// </summary>
public record struct Entry(ushort ResourceId, uint FileOffset);

/// <summary>
/// Represents an alias entry in the .pak file, mapping a resource ID to an entry index.
/// </summary>
public record struct Alias(ushort ResourceId, ushort EntryIndex);
