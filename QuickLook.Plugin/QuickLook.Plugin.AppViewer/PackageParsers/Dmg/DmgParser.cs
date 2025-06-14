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

namespace QuickLook.Plugin.AppViewer.PackageParsers.Dmg;

public static class DmgParser
{
    public static DmgInfo Parse(string path)
    {
        using DmgReader reader = new(path);

        return new DmgInfo()
        {
            DisplayName = reader.DisplayName,
            VersionName = reader.ShortVersionString,
            VersionCode = reader.Version,
            Identifier = reader.Identifier,
            MinimumOSVersion = reader.MinimumOSVersion,
            SupportedPlatforms = reader.SupportedPlatforms,
            PlatformVersion = reader.PlatformVersion,
            Permissions = [.. reader.InfoPlistDict.Keys.Where(key => key.StartsWith("NS") && key.EndsWith("UsageDescription"))],
            Logo = reader.Logo,
        };
    }
}
