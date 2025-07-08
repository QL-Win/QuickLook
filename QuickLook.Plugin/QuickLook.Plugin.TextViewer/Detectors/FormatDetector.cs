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

using System.Linq;

namespace QuickLook.Plugin.TextViewer.Detectors;

public class FormatDetector
{
    public static FormatDetector Instance { get; } = new();

    internal IFormatDetector[] TextDetectors =
    [
        new XMLDetector(),
        new JSONDetector(),
        new MakefileDetector(),
        //new CMakeListsDetector(),
    ];

    public static IFormatDetector Detect(string path, string text)
    {
        _ = path;

        if (string.IsNullOrWhiteSpace(text)) return null;

        return Instance.TextDetectors.Where(detector => detector.Detect(path, text))
            .FirstOrDefault();
    }
}

public interface IFormatDetector
{
    public string Name { get; }

    public string Extension { get; }

    public bool Detect(string path, string text);
}
