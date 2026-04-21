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

using WixToolset.Dtf.WindowsInstaller;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Msp;

public static class MspParser
{
    /// <summary>
    /// Reads metadata from an MSP patch file via the Summary Information stream.
    /// MSP files cannot be opened with <see cref="Database"/>; use
    /// <see cref="SummaryInfo"/> (wraps MsiGetSummaryInformation) instead.
    /// </summary>
    public static MspInfo Parse(string path)
    {
        MspInfo info = new();

        using var si = new SummaryInfo(path, enableWrite: false);

        // Subject is the human-readable patch name; fall back to Title if absent.
        info.DisplayName = string.IsNullOrWhiteSpace(si.Subject)
            ? (si.Title ?? string.Empty)
            : si.Subject;

        info.Description = si.Comments ?? string.Empty;
        info.Manufacturer = si.Author ?? string.Empty;
        info.PatchCode = si.RevisionNumber ?? string.Empty;

        return info;
    }
}
