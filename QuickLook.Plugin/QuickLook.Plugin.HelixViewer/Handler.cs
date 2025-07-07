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

using System.IO;
using System.Linq;

namespace QuickLook.Plugin.HelixViewer;

internal static class Handler
{
    public static bool CanHandle(string path)
    {
        var ext = Path.GetExtension(path).ToLower();

        if (ext == ".obj")
        {
            var firstLines = File.ReadLines(path).Take(10);
            foreach (var line in firstLines)
            {
                if (line.StartsWith("#") || line.StartsWith("v ") || line.StartsWith("f ") || line.StartsWith("o ") ||
                    line.StartsWith("vn ") || line.StartsWith("vt ") || line.StartsWith("mtllib"))
                {
                    return true;
                }
            }
        }
        else
        {
            return true; // Assume other formats are supported
        }

        return false;
    }
}
