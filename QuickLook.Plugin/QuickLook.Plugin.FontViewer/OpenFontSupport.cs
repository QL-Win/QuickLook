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

using QuickLook.Typography.OpenFont;
using System;
using System.IO;
using System.IO.Compression;
using Typography.OpenFont;
using Typography.OpenFont.WebFont;

namespace QuickLook.Plugin.FontViewer;

/// <summary>
/// Pure-C# OpenFont helpers via Typography (TTF/OTF/TTC/WOFF/WOFF2).
/// Replaces the former FreeType native dependency for family-name lookup.
/// </summary>
internal static class OpenFontSupport
{
    private static readonly object InitLock = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized) return;

        lock (InitLock)
        {
            if (_initialized) return;

            OurOpenFontSystemSetup.SetupWoff2DecompressFunctions();

            // WOFF 1.0 stores tables as zlib; net462 has DeflateStream but not ZLibStream
            WoffDefaultZlibDecompressFunc.DecompressHandler ??= static (compressed, output) =>
            {
                try
                {
                    if (compressed is null || output is null || compressed.Length < 2)
                        return false;

                    using var input = new MemoryStream(compressed, 2, compressed.Length - 2);
                    using var deflate = new DeflateStream(input, CompressionMode.Decompress);
                    using var ms = new MemoryStream(output.Length);
                    deflate.CopyTo(ms);

                    if (ms.Length != output.Length)
                        return false;

                    var bytes = ms.ToArray();
                    Buffer.BlockCopy(bytes, 0, output, 0, bytes.Length);
                    return true;
                }
                catch
                {
                    return false;
                }
            };

            _initialized = true;
        }
    }

    public static string GetFontFamilyName(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;

        try
        {
            EnsureInitialized();

            using var fs = File.OpenRead(path);
            var preview = new OpenFontReader().ReadPreview(fs);
            if (preview is null)
                return null;

            if (preview.IsFontCollection && preview.MemberCount > 0)
            {
                var member = preview.GetMember(0);
                if (!string.IsNullOrWhiteSpace(member?.Name))
                    return member.Name;
            }

            if (!string.IsNullOrWhiteSpace(preview.TypographicFamilyName))
                return preview.TypographicFamilyName;

            if (!string.IsNullOrWhiteSpace(preview.Name))
                return preview.Name;

            return null;
        }
        catch
        {
            return null;
        }
    }
}
