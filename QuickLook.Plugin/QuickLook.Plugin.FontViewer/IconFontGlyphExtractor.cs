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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Typography.OpenFont;

namespace QuickLook.Plugin.FontViewer;

/// <summary>
/// opentype.js cannot parse WOFF2. Extract glyph metadata via Typography instead,
/// and let the browser render the original WOFF2 through @font-face / FontFace.
/// </summary>
internal static class IconFontGlyphExtractor
{
    private static readonly Regex HexGlyphName = new(@"^[0-9A-Fa-f]{4,6}$", RegexOptions.Compiled);

    public static bool TryExtract(string path, out string familyName, out string iconsJson)
    {
        familyName = null;
        iconsJson = null;

        try
        {
            OpenFontSupport.EnsureInitialized();

            using var fs = File.OpenRead(path);
            var typeface = new OpenFontReader().Read(fs);
            if (typeface is null)
                return false;

            familyName = typeface.Name;
            if (string.IsNullOrWhiteSpace(familyName))
                familyName = Path.GetFileNameWithoutExtension(path);

            var nameByIndex = new Dictionary<ushort, string>();
            foreach (var map in typeface.GetGlyphNameIter())
            {
                if (!string.IsNullOrEmpty(map.glyphName))
                    nameByIndex[map.glyphIndex] = map.glyphName;
            }

            var unicodes = new List<uint>();
            typeface.CollectUnicode(unicodes);

            var icons = new List<string>(unicodes.Count);
            var seen = new HashSet<uint>();

            foreach (var codepoint in unicodes.OrderBy(u => u))
            {
                if (codepoint == 0 || !seen.Add(codepoint))
                    continue;

                var glyphIndex = typeface.GetGlyphIndex((int)codepoint);
                nameByIndex.TryGetValue(glyphIndex, out var glyphName);
                var name = FormatGlyphName(codepoint, glyphName);
                var ch = char.ConvertFromUtf32((int)codepoint);

                icons.Add(
                    "{" +
                    $"name:{EscapeJsonString(name)}," +
                    $"codepoint:{codepoint}," +
                    $"char:{EscapeJsonString(ch)}," +
                    $"index:{glyphIndex}" +
                    "}");
            }

            if (icons.Count == 0)
                return false;

            iconsJson = "[" + string.Join(",", icons) + "]";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string FormatGlyphName(uint codepoint, string glyphName)
    {
        glyphName ??= string.Empty;
        var trimmed = Regex.Replace(glyphName, @"^uni", "", RegexOptions.IgnoreCase);
        trimmed = Regex.Replace(trimmed, @"^u", "", RegexOptions.IgnoreCase);

        if (HexGlyphName.IsMatch(trimmed))
            return "u" + trimmed.ToLowerInvariant();

        if (!string.IsNullOrEmpty(glyphName) &&
            glyphName != ".notdef" &&
            !glyphName.StartsWith("glyph", StringComparison.OrdinalIgnoreCase))
        {
            return Regex.Replace(glyphName, @"\s+", "-").ToLowerInvariant();
        }

        return "u" + codepoint.ToString("x").PadLeft(4, '0');
    }

    private static string EscapeJsonString(string s)
    {
        var sb = new StringBuilder(s.Length + 2);
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                        sb.Append($"\\u{(int)c:x4}");
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}
