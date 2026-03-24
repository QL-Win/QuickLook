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

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using static QuickLook.Common.NativeMethods.MsCms;
using static QuickLook.Common.NativeMethods.User32;

namespace QuickLook.Common.Helpers;

public static class DisplayDeviceHelper
{
    public const int DefaultDpi = 96;

    public static ScaleFactor GetScaleFactorFromWindow(Window window)
    {
        return GetScaleFactorFromWindow(new WindowInteropHelper(window).EnsureHandle());
    }

    public static ScaleFactor GetCurrentScaleFactor()
    {
        return GetScaleFactorFromWindow(GetForegroundWindow());
    }

    public static ScaleFactor GetScaleFactorFromWindow(nint hwnd)
    {
        var dpiX = DefaultDpi;
        var dpiY = DefaultDpi;

        try
        {
            if (Environment.OSVersion.Version > new Version(6, 2)) // Windows 8.1 = 6.3.9200
            {
                var hMonitor = MonitorFromWindow(hwnd, MonitorDefaults.TOPRIMARY);
                GetDpiForMonitor(hMonitor, MonitorDpiType.EFFECTIVE_DPI, out dpiX, out dpiY);
            }
            else
            {
                using var g = Graphics.FromHwnd(IntPtr.Zero);
                var desktop = g.GetHdc();
                try
                {
                    dpiX = GetDeviceCaps(desktop, DeviceCap.LOGPIXELSX);
                    dpiY = GetDeviceCaps(desktop, DeviceCap.LOGPIXELSY);
                }
                finally
                {
                    g.ReleaseHdc(desktop);
                }
            }
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
        }

        return new ScaleFactor { Horizontal = (float)dpiX / DefaultDpi, Vertical = (float)dpiY / DefaultDpi };
    }

    public static string GetMonitorColorProfileFromWindow(Window window)
    {
        try
        {
            var hMonitor = MonitorFromWindow(new WindowInteropHelper(window).EnsureHandle(), MonitorDefaults.TONEAREST);
            return GetMonitorColorProfile(hMonitor);
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x80263001))
        {
            // Desktop composition is disabled (e.g., during eGPU reconnection)
            ProcessHelper.WriteLog("Failed to get color profile: Desktop composition is disabled. This is expected during display reconfiguration.");
            return null;
        }
        catch (Exception ex)
        {
            ProcessHelper.WriteLog($"Failed to get monitor color profile: {ex}");
            return null;
        }
    }

    public static string GetMonitorColorProfile(nint hMonitor)
    {
        var profileDir = new StringBuilder(255);
        var pDirSize = (uint)profileDir.Capacity;
        GetColorDirectory(null, profileDir, ref pDirSize);

        var mInfo = new MONITORINFOEX();
        mInfo.cbSize = (uint)Marshal.SizeOf(mInfo);
        if (!GetMonitorInfo(hMonitor, ref mInfo))
            return null;

        var dd = new DISPLAYDEVICE();
        dd.cb = (uint)Marshal.SizeOf(dd);
        if (!EnumDisplayDevices(mInfo.szDevice, 0, ref dd, 0))
            return null;

        WcsGetUsePerUserProfiles(dd.DeviceKey, CLASS_MONITOR, out bool usePerUserProfiles);
        var scope = usePerUserProfiles ? WcsProfileManagementScope.CURRENT_USER : WcsProfileManagementScope.SYSTEM_WIDE;

        if (!WcsGetDefaultColorProfileSize(scope, dd.DeviceKey, ColorProfileType.ICC, ColorProfileSubtype.NONE, 0, out uint size))
            return null;

        var profileName = new StringBuilder((int)size);
        if (!WcsGetDefaultColorProfile(scope, dd.DeviceKey, ColorProfileType.ICC, ColorProfileSubtype.NONE, 0, size, profileName))
            return null;
        return System.IO.Path.Combine(profileDir.ToString(), profileName.ToString());
    }

    [DllImport("shcore.dll")]
    private static extern uint
        GetDpiForMonitor(nint hMonitor, MonitorDpiType dpiType, out int dpiX, out int dpiY);

    private enum MonitorDpiType
    {
        EFFECTIVE_DPI = 0,
        ANGULAR_DPI = 1,
        RAW_DPI = 2,
    }

    public struct ScaleFactor
    {
        public float Horizontal;
        public float Vertical;
    }
}
