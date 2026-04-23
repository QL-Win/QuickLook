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

namespace QuickLook.Plugin.TextViewer.Detectors;

/// <summary>
/// Detect whether a text file without suffix is ​​a shell script file
/// </summary>
public sealed class ShellScriptDetector : IFormatDetector
{
    public string Name => "Shell Script";

    public string Extension => ".sh";

    public bool Detect(string path, string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        // Only handle files without extension
        if (Path.GetExtension(path) != string.Empty)
            return false;

        ReadOnlySpan<char> span = text.AsSpan();

        int i = 0;

        // Skip UTF-8 BOM (\uFEFF) if present
        if (span.Length > 0 && span[0] == '\uFEFF')
            i = 1;

        // Must start with shebang "#!"
        if (span.Length < i + 2 || span[i] != '#' || span[i + 1] != '!')
            return false;

        i += 2;

        // Skip whitespace after shebang
        while (i < span.Length && (span[i] == ' ' || span[i] == '\t'))
            i++;

        // Read the first line only
        int start = i;
        while (i < span.Length && span[i] != '\n' && span[i] != '\r')
            i++;

        var line = span.Slice(start, i - start).Trim();

        // Case 1: direct interpreter path (e.g. /bin/bash, /bin/sh)
        if (line.Length >= 2 &&
            line[line.Length - 2] == 's' &&
            line[line.Length - 1] == 'h')
            return true;

        // Case 2: env style (e.g. /usr/bin/env bash)
        int lastSpace = line.LastIndexOf(' ');
        if (lastSpace >= 0)
        {
            var lastToken = line.Slice(lastSpace + 1);

            if (lastToken.Length >= 2 &&
                lastToken[lastToken.Length - 2] == 's' &&
                lastToken[lastToken.Length - 1] == 'h')
                return true;
        }

        return false;
    }
}
