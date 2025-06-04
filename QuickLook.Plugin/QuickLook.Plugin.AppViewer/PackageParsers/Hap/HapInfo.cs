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

namespace QuickLook.Plugin.AppViewer.PackageParsers.Hap;

public sealed class HapInfo
{
    public string Label { get; set; }

    public string Icon { get; set; }

    public byte[] Logo { get; set; }

    public byte[] AppIconForeground { get; set; }

    public byte[] AppIconBackground { get; set; }

    public bool HasIcon { get; set; } = false;

    public bool HasLayeredIcon { get; set; } = false;

    public string VersionName { get; set; }

    public string VersionCode { get; set; }

    public string CompileSdkType { get; set; }

    public string CompileSdkVersion { get; set; }

    public string MinAPIVersion { get; set; }

    public string TargetAPIVersion { get; set; }

    public string BundleName { get; set; }

    public bool Debug { get; set; }

    public string[] RequestPermissions { get; set; } = [];

    public string[] DeviceTypes { get; set; } = [];
}
