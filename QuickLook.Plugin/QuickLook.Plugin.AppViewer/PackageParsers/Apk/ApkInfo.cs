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

namespace QuickLook.Plugin.AppViewer.PackageParsers.Apk;

public class ApkInfo
{
    public string VersionName { get; set; }

    public string VersionCode { get; set; }

    public string TargetSdkVersion { get; set; }

    public List<string> Permissions { get; set; } = [];

    public string PackageName { get; set; }

    public string MinSdkVersion { get; set; }

    public string Icon { get; set; }

    public Dictionary<string, string> Icons { get; set; } = [];

    public byte[] Logo { get; set; }

    public string Label { get; set; }

    public Dictionary<string, string> Labels { get; set; } = [];

    public bool HasIcon
    {
        get
        {
            if (Icons.Count <= 0)
            {
                return !string.IsNullOrEmpty(Icon);
            }

            return true;
        }
    }

    public List<string> Locales { get; set; } = [];

    public List<string> Densities { get; set; } = [];

    public string LaunchableActivity { get; set; }

    public string[] ABIs { get; set; } = [];
}
