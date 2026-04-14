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

using System.Runtime.InteropServices;
using System.Text;

namespace QuickLook.Common.NativeMethods;

public static class MsCms
{
    [DllImport("mscms.dll", CharSet = CharSet.Auto)]
    public static extern bool GetColorDirectory(
        [MarshalAs(UnmanagedType.LPWStr)] string pMachineName,
        StringBuilder pBuffer,
        ref uint pdwSize);

    [DllImport("mscms.dll", CharSet = CharSet.Auto)]
    public static extern bool WcsGetUsePerUserProfiles(
        [MarshalAs(UnmanagedType.LPTStr)] string deviceName,
        uint deviceClass,
        out bool usePerUserProfiles);

    [DllImport("mscms.dll", CharSet = CharSet.Auto)]
    public static extern bool WcsGetDefaultColorProfileSize(
        WcsProfileManagementScope scope,
        [MarshalAs(UnmanagedType.LPTStr)] string deviceName,
        ColorProfileType colorProfileType,
        ColorProfileSubtype colorProfileSubType,
        uint dwProfileID,
        out uint cbProfileName);

    [DllImport("mscms.dll", CharSet = CharSet.Auto)]
    public static extern bool WcsGetDefaultColorProfile(
        WcsProfileManagementScope scope,
        [MarshalAs(UnmanagedType.LPTStr)] string deviceName,
        ColorProfileType colorProfileType,
        ColorProfileSubtype colorProfileSubType,
        uint dwProfileID,
        uint cbProfileName,
        StringBuilder pProfileName);

    public enum WcsProfileManagementScope
    {
        SYSTEM_WIDE,
        CURRENT_USER,
    }

    public enum ColorProfileType
    {
        ICC,
        DMP,
        CAMP,
        GMMP,
    };

    public enum ColorProfileSubtype
    {
        PERCEPTUAL,
        RELATIVE_COLORIMETRIC,
        SATURATION,
        ABSOLUTE_COLORIMETRIC,
        NONE,
        RGB_WORKING_SPACE,
        CUSTOM_WORKING_SPACE,
        STANDARD_DISPLAY_COLOR_MODE,
        EXTENDED_DISPLAY_COLOR_MODE,
    };

    public const uint CLASS_MONITOR = 0x6d6e7472; // 'mntr'
    public const uint CLASS_PRINTER = 0x70727472; // 'prtr'
    public const uint CLASS_SCANNER = 0x73636e72; // 'scnr'
}
