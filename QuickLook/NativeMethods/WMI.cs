// Copyright © 2024 QL-Win Contributors
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace QuickLook.NativeMethods;

internal static class WMI
{
    public static List<string> GetGPUNames()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\CIMV2", @"SELECT * FROM Win32_VideoController");
            List<string> names = [];

            foreach (var obj in searcher.Get())
                names.Add(obj["Name"] as string);

            return names;
        }
        catch (ManagementException e)
        {
            Debug.WriteLine($"ManagementException caught: {e.Message}");
        }
        catch (COMException e)
        {
            Debug.WriteLine($"COMException caught: {e.Message}");
        }
        catch (Exception e)
        {
            Debug.WriteLine($"General exception caught: {e.Message}");
        }
        return [];
    }
}
