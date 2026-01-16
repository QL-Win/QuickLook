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

using System.Linq;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Ipa;

public static class IpaParser
{
    public static IpaInfo Parse(string path)
    {
        IpaReader reader = new(path);

        return new IpaInfo()
        {
            DisplayName = reader.DisplayName,
            VersionName = reader.ShortVersionString,
            VersionCode = reader.Version,
            Identifier = reader.Identifier,
            DeviceFamily = reader.DeviceFamily,
            MinimumOSVersion = reader.MinimumOSVersion,
            PlatformVersion = reader.PlatformVersion,
            Permissions = [.. reader.InfoPlistDict.Keys.Where(key => key.StartsWith("NS") && key.EndsWith("UsageDescription"))],
            Logo = reader.Icon,
        };
    }
}
