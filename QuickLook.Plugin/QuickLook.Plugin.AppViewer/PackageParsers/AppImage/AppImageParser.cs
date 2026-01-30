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

namespace QuickLook.Plugin.AppViewer.PackageParsers.AppImage;

public class AppImageParser
{
    public static AppImageInfo Parse(string path)
    {
        var reader = new AppImageReader(path);

        return new AppImageInfo()
        {
            Arch = reader.Arch,
            Version = reader.Version,
            Name = reader.Name,
            Exec = reader.Exec,
            Icon = reader.Icon,
            Logo = reader.Logo,
            Type = reader.Type,
            Terminal = reader.Terminal,
            Env = reader.Env,
        };
    }
}
