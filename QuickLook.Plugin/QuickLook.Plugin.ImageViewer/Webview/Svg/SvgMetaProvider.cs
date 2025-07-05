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
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;

namespace QuickLook.Plugin.ImageViewer.Webview.Svg;

public class SvgMetaProvider(string path) : IWebMetaProvider
{
    private readonly string _path = path;
    private Size _size = Size.Empty;

    public Size GetSize()
    {
        if (_size != Size.Empty)
        {
            return _size;
        }

        if (!File.Exists(_path))
        {
            return _size;
        }

        try
        {
            var svgContent = File.ReadAllText(_path);
            var svg = XElement.Parse(svgContent);
            XNamespace ns = svg.Name.Namespace;

            string widthAttr = svg.Attribute("width")?.Value;
            string heightAttr = svg.Attribute("height")?.Value;

            float? width = TryParseSvgLength(widthAttr);
            float? height = TryParseSvgLength(heightAttr);

            if (width.HasValue && height.HasValue)
            {
                _size = new Size { Width = width.Value, Height = height.Value };
            }

            string viewBoxAttr = svg.Attribute("viewBox")?.Value;
            if (!string.IsNullOrEmpty(viewBoxAttr))
            {
                var parts = viewBoxAttr.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 4 &&
                    float.TryParse(parts[2], out float vbWidth) &&
                    float.TryParse(parts[3], out float vbHeight))
                {
                    _size = new Size { Width = vbWidth, Height = vbHeight };
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        return _size;
    }

    private static float? TryParseSvgLength(string input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        var match = Regex.Match(input.Trim(), @"^([\d.]+)(px|pt|mm|cm|in|em|ex|%)?$", RegexOptions.IgnoreCase);
        if (match.Success && float.TryParse(match.Groups[1].Value, out float value))
        {
            return value;
        }
        return null;
    }
}
