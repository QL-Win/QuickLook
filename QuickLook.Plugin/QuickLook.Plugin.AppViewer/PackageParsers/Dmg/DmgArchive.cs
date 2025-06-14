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

using DiscUtils.HfsPlus;
using System;
using System.IO;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Dmg;

public class DmgArchive : IDisposable
{
    public string Entry { get; set; }

    public HfsPlusFileSystem FileSystem { get; set; }

    public void Dispose()
    {
        FileSystem?.Dispose();
        FileSystem = null;
    }

    public byte[] GetBytes()
    {
        if (Entry is null)
            return null;

        if (FileSystem is null)
            return null;

        using var stream = FileSystem.OpenFile(Entry, FileMode.Open, FileAccess.Read);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        byte[] fileBytes = ms.ToArray();

        return fileBytes;
    }
}
