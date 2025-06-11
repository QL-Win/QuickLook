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

using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace QuickLook.Plugin.OfficeViewer;

internal static class CLSIDRegister
{
    public const string Office = "{84F66100-FF7C-4fb4-B0C0-02CD7FB668FE}";
    public const string Visio = "{279D6C9A-652E-4833-BEFC-312CA8887857}";

    public static string GetName(string clsid)
    {
        try
        {
            // Such as `Computer\HKEY_CLASSES_ROOT\CLSID\{84F66100-FF7C-4fb4-B0C0-02CD7FB668FE}`
            string displayName = Registry.GetValue($@"HKEY_CLASSES_ROOT\CLSID\{clsid}", string.Empty, null)?.ToString();
            return displayName;
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error reading registry: " + e.Message);
        }
        return null;
    }
}
