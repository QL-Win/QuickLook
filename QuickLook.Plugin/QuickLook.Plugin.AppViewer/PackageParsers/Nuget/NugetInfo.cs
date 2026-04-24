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

using System.Drawing;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Nuget;

public class NugetInfo
{
    public string PackageId { get; set; }

    public string Version { get; set; }

    public string Authors { get; set; }

    public string Description { get; set; }

    public string ProjectUrl { get; set; }

    public string RepositoryUrl { get; set; }

    public string License { get; set; }

    public Bitmap Icon { get; set; }

    public string[] TargetFrameworks { get; set; } = [];

    public string[] Dependencies { get; set; } = [];
}
