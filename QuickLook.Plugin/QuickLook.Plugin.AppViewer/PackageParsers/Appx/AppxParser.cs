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

namespace QuickLook.Plugin.AppViewer.PackageParsers.Appx;

public static class AppxParser
{
    public static AppxInfo Parse(string path)
    {
        bool isBundle = Path.GetExtension(path).ToLower() switch
        {
            ".msixbundle" or ".appxbundle" => true,
            _ => false,
        };

        if (isBundle)
        {
            AppxBundleReader appxReader = new(path);

            return new AppxInfo
            {
                ProductName = appxReader.DisplayName,
                ProductVersion = appxReader.Version,
                Publisher = appxReader.Publisher,
                Logo = appxReader.Icon,
                Capabilities = appxReader.Capabilities,
            };
        }
        else
        {
            AppxReader appxReader = new(path);

            return new AppxInfo
            {
                ProductName = appxReader.DisplayName,
                ProductVersion = appxReader.Version,
                Publisher = appxReader.Publisher,
                Logo = appxReader.Icon,
                Capabilities = appxReader.Capabilities,
            };
        }
    }
}
