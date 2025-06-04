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

namespace QuickLook.Plugin.AppViewer.PackageParsers.Ipa;

public class IpaInfo
{
    public string DisplayName { get; set; }

    public string VersionName { get; set; }

    public string VersionCode { get; set; }

    public string Identifier { get; set; }

    public string MinimumOSVersion { get; set; }

    public string PlatformVersion { get; set; }

    public string DeviceFamily { get; set; }

    public string[] Permissions { get; set; } = [];

    public byte[] Logo { get; set; }

    public bool HasIcon => Logo?.Any() ?? false;
}
