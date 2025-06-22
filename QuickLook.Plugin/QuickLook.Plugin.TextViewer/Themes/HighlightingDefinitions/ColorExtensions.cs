// Copyright © 2017-2025 QL-Win Contributors
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
using System.Globalization;
using System.Windows.Media;

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions;

internal static class ColorExtensions
{
    public static Color ToColor(this string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            throw new ArgumentNullException(nameof(hex));

        hex = hex.TrimStart('#');

        if (hex.Length == 6)
            hex = "FF" + hex;

        if (hex.Length != 8)
            throw new FormatException("Hex color must be 6 (RGB) or 8 (ARGB) characters long.");

        byte a = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
        byte r = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);

        return Color.FromArgb(a, r, g, b);
    }

    public static Brush ToBrush(this Color color)
    {
        return new SolidColorBrush(color);
    }

    public static Brush ToBrush(this string hex)
    {
        return new SolidColorBrush(hex.ToColor());
    }
}
