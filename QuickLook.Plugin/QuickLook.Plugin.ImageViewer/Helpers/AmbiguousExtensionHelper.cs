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

namespace QuickLook.Plugin.ImageViewer.Helpers;

/// <summary>
/// Routes extensions shared by multiple plugins based on file content.
/// </summary>
public static class AmbiguousExtensionHelper
{
    private static readonly HashSet<string> AmbiguousExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mdc", // Cursor Markdown rules vs Minolta RAW image
    };

    public enum Route
    {
        Unknown,
        Image,
        Text,
    }

    public static bool IsAmbiguousExtension(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        string extension = Path.GetExtension(path);
        return !string.IsNullOrEmpty(extension) && AmbiguousExtensions.Contains(extension);
    }

    public static Route GetRoute(string path)
    {
        if (!IsAmbiguousExtension(path))
            return Route.Unknown;

        if (IsLikelyText(path))
            return Route.Text;

        return Route.Image;
    }

    public static bool IsLikelyText(string path)
    {
        try
        {
            if (!File.Exists(path))
                return false;

            // Read the first 16KB, check if we can get something
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            const int bufferLength = 16 * 1024;
            byte[] buffer = new byte[bufferLength];
            int size = fs.Read(buffer, 0, bufferLength);

            if (size == 0)
                return true;

            for (int i = 1; i < size; i++)
                if (buffer[i - 1] == 0 && buffer[i] == 0)
                    return false;

            return true;
        }
        catch
        {
            return false;
        }
    }
}
