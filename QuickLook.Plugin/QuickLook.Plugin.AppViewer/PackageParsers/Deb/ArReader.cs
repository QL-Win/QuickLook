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

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Deb;

public static class ArReader
{
    public static ArEntry[] Read(string filePath)
    {
        var entries = new List<ArEntry>();

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        // Check ar magic
        var magic = br.ReadBytes(8);
        if (Encoding.ASCII.GetString(magic) != "!<arch>\n")
            throw new InvalidDataException("Not a valid ar file");

        while (fs.Position < fs.Length)
        {
            var header = br.ReadBytes(60);
            if (header.Length < 60)
                break;

            string name = Encoding.ASCII.GetString(header, 0, 16).Trim();
            string sizeStr = Encoding.ASCII.GetString(header, 48, 10).Trim();
            string fmag = Encoding.ASCII.GetString(header, 58, 2);
            if (fmag != "`\n")
                throw new InvalidDataException("Invalid header end");

            long size = long.Parse(sizeStr);

            byte[] data = br.ReadBytes((int)size);

            // Completion alignment: ar file is evenly aligned
            if (size % 2 != 0)
                br.ReadByte();

            entries.Add(new ArEntry
            {
                FileName = name,
                FileSize = size,
                Data = data
            });
        }

        return [.. entries];
    }
}

public class ArEntry
{
    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public byte[] Data { get; set; } = [];

    public override string ToString()
    {
        return $"{FileName} ({FileSize})";
    }
}
