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
using System.Linq;
using System.Text;

namespace QuickLook.Plugin.HelixViewer;

internal static class Handler
{
    public static bool CanHandle(string path)
    {
        var ext = Path.GetExtension(path).ToLower();

        // Simple solution to doubts
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
#if S_DXF
        else if (ext == ".dxf")
        {
            using var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            const int bufferLength = 16 * 1024;
            var buffer = new byte[bufferLength];
            int size = s.Read(buffer, 0, buffer.Length);

            for (int i = 0; i < size - 1; i++)
            {
                if (buffer[i] == (byte)'3' && buffer[i + 1] == (byte)'D')
                {
                    return true;
                }
            }
        }
#endif
        else
        {
            // Assume other formats are supported
            return true;
        }

        return false;
    }
}
