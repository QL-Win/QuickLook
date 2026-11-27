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

namespace QuickLook.Common.Helpers;

public static class PathHelper
{
    private static readonly char[] TrimmableExtensionChars = ['"', ' ', '\t', '\r', '\n'];

    public static string GetExtension(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        var normalized = Normalize(path);

        // Path.GetExtension validates the whole path through Path.CheckInvalidPathChars
        // and can throw for shell-provided paths. Parse UNC paths directly so they
        // always reach the preview pipeline.
        if (IsUncPath(normalized))
            return GetExtensionFallback(normalized);

        try
        {
            return Path.GetExtension(normalized);
        }
        catch (ArgumentException)
        {
            return GetExtensionFallback(normalized);
        }
    }

    public static string GetFileName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        var normalized = Normalize(path);

        try
        {
            return Path.GetFileName(normalized);
        }
        catch (ArgumentException)
        {
            var separator = normalized.LastIndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]);
            return separator < 0 ? normalized : normalized.Substring(separator + 1);
        }
    }

    public static string GetPathRoot(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        var normalized = Normalize(path);

        try
        {
            return Path.GetPathRoot(normalized);
        }
        catch (ArgumentException)
        {
            if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
            {
                var parts = normalized.Substring(2).Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], 3);
                return parts.Length switch
                {
                    0 => string.Empty,
                    1 => @"\\" + parts[0],
                    _ => @"\\" + parts[0] + @"\" + parts[1],
                };
            }

            if (normalized.Length >= 2 && normalized[1] == Path.VolumeSeparatorChar)
                return normalized.Substring(0, 2) + Path.DirectorySeparatorChar;

            return string.Empty;
        }
    }

    public static DateTime? GetLastWriteTime(string path)
    {
        try
        {
            var last = File.GetLastWriteTime(path);
            if (last.Year == 1601)
            {
                // File.GetLastWriteTime returns the 1601 sentinel for missing or unreadable paths.
                return null;
            }

            return last;
        }
        catch (Exception e) when (e is ArgumentException ||
                                  e is UnauthorizedAccessException ||
                                  e is IOException ||
                                  e is NotSupportedException)
        {
            // An unusable path must not hide the rest of the info card.
            return null;
        }
    }

    public static bool IsShortcut(string path)
    {
        return GetExtension(path).Equals(".lnk", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsUncPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var normalized = Normalize(path);
        return normalized.Length >= 2 &&
               normalized[0] == Path.DirectorySeparatorChar &&
               normalized[1] == Path.DirectorySeparatorChar;
    }

    private static string Normalize(string path)
    {
        path = path.Trim();

        if (path.Length >= 2 && path[0] == '"' && path[path.Length - 1] == '"')
            path = path.Substring(1, path.Length - 2).Trim();

        return path;
    }

    private static string GetExtensionFallback(string path)
    {
        var end = path.Length - 1;

        while (end >= 0 && char.IsWhiteSpace(path[end]))
            end--;

        for (var i = end; i >= 0; i--)
        {
            var c = path[i];

            if (c == '.')
                return i == end
                    ? string.Empty
                    : path.Substring(i, end - i + 1).TrimEnd(TrimmableExtensionChars);

            if (c == Path.DirectorySeparatorChar ||
                c == Path.AltDirectorySeparatorChar ||
                c == Path.VolumeSeparatorChar)
                break;
        }

        return string.Empty;
    }
}
