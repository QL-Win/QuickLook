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

namespace QuickLook.Plugin.AppViewer.PackageParsers.Msi;

public static class MsiParser
{
    public static MsiInfo Parse(string path)
    {
        MsiInfo info = new();
        using var database = new Database(path, DatabaseOpenMode.ReadOnly);

        info.ProductVersion = database.ReadPropertyString(nameof(MsiInfo.ProductVersion));
        info.ProductName = database.ReadPropertyString(nameof(MsiInfo.ProductName));
        info.Manufacturer = database.ReadPropertyString(nameof(MsiInfo.Manufacturer));
        info.ProductCode = database.ReadPropertyString(nameof(MsiInfo.ProductCode));
        info.UpgradeCode = database.ReadPropertyString(nameof(MsiInfo.UpgradeCode));

        return info;
    }

    private static string ReadPropertyString(this Database database, string propertyName)
    {
        using View view = database.OpenView($"SELECT `Value` FROM `Property` WHERE `Property`='{propertyName}'");
        view.Execute(null);
        using Record record = view.Fetch();
        return record?.GetString(1);
    }
}
