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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using QuickLook.Common.Helpers;

namespace QuickLook.Common.NativeMethods;

public static class User32
{
    public delegate int KeyboardHookProc(int code, int wParam, ref KeyboardHookStruct lParam);

    [DllImport("user32.dll")]
    public static extern int MoveWindow(nint hWnd, int x, int y, int nWidth, int nHeight,
        [MarshalAs(UnmanagedType.Bool)] bool bRepaint);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    public static extern nint SetWindowsHookEx(int idHook, KeyboardHookProc callback, nint hInstance,
        uint threadId);

    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(nint hInstance);

    [DllImport("user32.dll")]
    public static extern int CallNextHookEx(nint idHook, int nCode, int wParam, ref KeyboardHookStruct lParam);

    [DllImport("user32.dll")]
    public static extern nint SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern nint GetAncestor(nint hwnd, uint gaFlags);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern nint GetDesktopWindow();

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(nint hwnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern nint GetWindowThreadProcessId(nint hWnd, nint processId);

    [DllImport("user32.dll")]
    public static extern nint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("user32.dll")]
    public static extern nint AttachThreadInput(nint idAttach, nint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    public static extern nint GetFocus();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern nint GetParent(nint hWnd);

    [DllImport("user32.dll")]
    public static extern int SetWindowCompositionAttribute(nint hwnd,
        ref WindowHelper.WindowCompositionAttributeData data);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
    public static extern int GetDeviceCaps(nint hDC, DeviceCap nIndex);

    [DllImport("user32.dll")]
    public static extern nint MonitorFromWindow(nint hWnd, MonitorDefaults dwFlags);

    [DllImport("user32.dll")]
    public extern static bool GetMonitorInfo(nint hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAYDEVICE lpDisplayDevice, uint dwFlags);

    public enum MonitorDefaults
    {
        TONULL = 0,
        TOPRIMARY = 1,
        TONEAREST = 2
    }

    public enum DeviceCap
    {
        /// <summary>
        /// Logical pixels inch in X
        /// </summary>
        LOGPIXELSX = 88,

        /// <summary>
        /// Logical pixels inch in Y
        /// </summary>
        LOGPIXELSY = 90
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct KeyboardHookStruct
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public int dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFOEX
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct DISPLAYDEVICE
    {
        public uint cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public uint StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    // ReSharper disable InconsistentNaming
    public static readonly nint HWND_TOPMOST = -1;

    public static readonly nint HWND_NOTOPMOST = -2;
    public static readonly nint HWND_TOP = 0;
    public static readonly nint HWND_BOTTOM = 1;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOREDRAW = 0x0008;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_DRAWFRAME = 0x0020;
    public const uint SWP_FRAMECHANGED = 0x0020;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const uint SWP_HIDEWINDOW = 0x0080;
    public const uint SWP_NOCOPYBITS = 0x0100;
    public const uint SWP_NOOWNERZORDER = 0x0200;
    public const uint SWP_NOREPOSITION = 0x0200;
    public const uint SWP_NOSENDCHANGING = 0x0400;
    public const uint SWP_DEFERERASE = 0x2000;
    public const uint SWP_ASYNCWINDOWPOS = 0x4000;

    public const int WH_KEYBOARD_LL = 13;
    public const int WM_KEYDOWN = 0x100;
    public const int WM_KEYUP = 0x101;
    public const int WM_SYSKEYDOWN = 0x104;
    public const int WM_SYSKEYUP = 0x105;
    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;
    public const int WS_SYSMENU = 0x00080000;
    public const int WS_MINIMIZEBOX = 0x00020000;
    public const int WS_MAXIMIZEBOX = 0x00010000;
    public const int WS_EX_NOACTIVATE = 0x08000000;

    public const uint GA_PARENT = 1;
    public const uint GA_ROOT = 2;
    public const uint GA_ROOTOWNER = 3;
    // ReSharper restore InconsistentNaming
}
