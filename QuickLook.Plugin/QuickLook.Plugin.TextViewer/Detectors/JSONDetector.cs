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
using System.Text.RegularExpressions;

namespace QuickLook.Plugin.TextViewer.Detectors;

public sealed class JSONDetector : IFormatDetector
{
    internal Regex Signature { get; } = new(@"""[^""]+""\s*:", RegexOptions.IgnoreCase);

    public string Name => "JSON";

    public string Extension => ".json";

    public bool Detect(string path, string text)
    {
        _ = path;

        if (string.IsNullOrWhiteSpace(text)) return false;

        var span = text.AsSpan();

        // Remove UTF-8 BOM if present
        if (span.Length > 0 && span[0] == '\uFEFF')
            span = span.Slice(1);

        // TrimStart
        int start = 0;
        while (start < span.Length && char.IsWhiteSpace(span[start]))
            start++;

        if (start >= span.Length)
            return false;

        if (span[start] != '{' && span[start] != '[')
            return false;

        // TrimEnd
        int end = span.Length - 1;
        while (end >= 0 && char.IsWhiteSpace(span[end]))
            end--;

        if (end < 0 || (span[end] != '}' && span[end] != ']'))
            return false;

        return Signature.IsMatch(text);
    }
}
