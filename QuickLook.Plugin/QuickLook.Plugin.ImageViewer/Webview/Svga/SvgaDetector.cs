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

using System.IO;

namespace QuickLook.Plugin.ImageViewer.Webview.Svga;

internal enum SvgaVersion
{
    V1,
    V2,
}

internal static class SvgaDetector
{
    public static SvgaVersion Detect(string path)
    {
        if (!File.Exists(path))
        {
            return SvgaVersion.V2;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Detect(stream);
    }

    public static SvgaVersion Detect(Stream stream)
        => IsZipArchive(stream) ? SvgaVersion.V1 : SvgaVersion.V2;

    /// <summary>
    /// SVGA 1.x files are ZIP archives (PK header).
    /// </summary>
    private static bool IsZipArchive(Stream stream)
    {
        var originalPosition = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);

        var header = new byte[2];
        var bytesRead = stream.Read(header, 0, 2);

        stream.Seek(originalPosition, SeekOrigin.Begin);

        return bytesRead == 2 && header[0] == 0x50 && header[1] == 0x4B;
    }
}
