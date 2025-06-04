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

namespace QuickLook.Plugin.AppViewer.PackageParsers.Deb;

public static class DebParser
{
    public static DebInfo Parse(string path)
    {
        DebReader reader = new(path);
        DebInfo info = new();

        {
            if (reader.ControlDict.TryGetValue("Package", out string value))
            {
                info.Package = value;
            }
        }

        {
            if (reader.ControlDict.TryGetValue("Maintainer", out string value))
            {
                info.Maintainer = value;
            }
        }

        {
            if (reader.ControlDict.TryGetValue("Uploaders", out string value))
            {
                info.Uploaders = value;
            }
        }

        {
            if (reader.ControlDict.TryGetValue("Version", out string value))
            {
                info.Version = value;
            }
        }

        {
            if (reader.ControlDict.TryGetValue("Architecture", out string value))
            {
                info.Architecture = value;
            }
        }

        {
            if (reader.ControlDict.TryGetValue("Description", out string value))
            {
                info.Description = value;
            }
        }

        return info;
    }
}
