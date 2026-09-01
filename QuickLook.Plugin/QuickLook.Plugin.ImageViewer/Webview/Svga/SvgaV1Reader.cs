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
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace QuickLook.Plugin.ImageViewer.Webview.Svga;

/// <summary>
/// Reads metadata from SVGA 1.x files (ZIP archive containing JSON movie.spec).
/// </summary>
internal static class SvgaV1Reader
{
    public static Size GetSize(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var specBytes = ExtractMovieSpec(stream);
        return ParseSize(specBytes);
    }

    private static byte[] ExtractMovieSpec(Stream svgaFileBuffer)
    {
        svgaFileBuffer.Seek(0, SeekOrigin.Begin);

        using var archive = new ZipArchive(svgaFileBuffer, ZipArchiveMode.Read, leaveOpen: true);

        foreach (var entry in archive.Entries)
        {
            if (entry.Name.Equals("movie.spec", StringComparison.OrdinalIgnoreCase))
            {
                return ReadEntryBytes(entry);
            }
        }

        foreach (var entry in archive.Entries)
        {
            if (entry.Name.EndsWith(".spec", StringComparison.OrdinalIgnoreCase))
            {
                return ReadEntryBytes(entry);
            }
        }

        throw new InvalidDataException("No valid SVGA 1.x data found in ZIP archive");
    }

    private static byte[] ReadEntryBytes(ZipArchiveEntry entry)
    {
        using var entryStream = entry.Open();
        using var memoryStream = new MemoryStream();
        entryStream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private static Size ParseSize(byte[] specBytes)
    {
        var json = Encoding.UTF8.GetString(specBytes);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("movie", out var movie)
            || !movie.TryGetProperty("viewBox", out var viewBox))
        {
            return Size.Empty;
        }

        var width = viewBox.GetProperty("width").GetSingle();
        var height = viewBox.GetProperty("height").GetSingle();
        return new Size(width, height);
    }
}
