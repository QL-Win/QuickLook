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

using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.Linq;

namespace QuickLook.Plugin.AppViewer.ApkPackageParser;

public static class ApkParser
{
    public static ApkInfo Parse(string path)
    {
        byte[] manifestData = null;
        byte[] resourcesData = null;

        using var zip = new ZipFile(path);

        // AndroidManifest.xml
        {
            ZipEntry entry = zip.GetEntry("AndroidManifest.xml");
            using var s = new BinaryReader(zip.GetInputStream(entry));
            manifestData = s.ReadBytes((int)entry.Size);
        }

        // resources.arsc
        {
            ZipEntry entry = zip.GetEntry("resources.arsc");
            using var s = new BinaryReader(zip.GetInputStream(entry));
            resourcesData = s.ReadBytes((int)entry.Size);
        }

        ApkReader apkReader = new();
        ApkInfo info = apkReader.ExtractInfo(manifestData, resourcesData);

        // Logo
        if (info.HasIcon)
        {
            ZipEntry entry = zip.GetEntry(info.IconFileName.LastOrDefault());
            using var s = new BinaryReader(zip.GetInputStream(entry));
            info.Logo = s.ReadBytes((int)entry.Size);
        }

        return info;
    }
}
