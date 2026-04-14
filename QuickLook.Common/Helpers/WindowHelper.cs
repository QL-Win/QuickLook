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

using QuickLook.Common.NativeMethods;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

namespace QuickLook.Common.Helpers;

public static class WindowHelper
{
    public enum WindowCompositionAttribute
    {
        WcaAccentPolicy = 19,
    }

    public static Size GetCurrentDesktopSize()
    {
        var scale = DisplayDeviceHelper.GetCurrentScaleFactor();
        var rect = GetCurrentDesktopRectInPixel();

        return new Size(rect.Width / scale.Horizontal, rect.Height / scale.Vertical);
    }

    public static Rect GetCurrentDesktopRectInPixel()
    {
        return GetDesktopRectFromWindowInPixel(User32.GetForegroundWindow());
    }

    public static Rect GetDesktopRectFromWindowInPixel(Window window)
    {
        return GetDesktopRectFromWindowInPixel(new WindowInteropHelper(window).Handle);
    }

    public static Rect GetDesktopRectFromWindowInPixel(nint hwnd)
    {
        var screen = Screen.FromHandle(hwnd).WorkingArea;

        var area = new Rect(new Point(screen.X, screen.Y),
            new Size(screen.Width, screen.Height));

        return area;
    }

    public static void BringToFront(this Window window, bool keep)
    {
        var handle = new WindowInteropHelper(window).Handle;
        keep |= window.Topmost;

        User32.SetWindowPos(handle, User32.HWND_TOPMOST, 0, 0, 0, 0,
            User32.SWP_NOMOVE | User32.SWP_NOSIZE | User32.SWP_NOACTIVATE);

        if (!keep)
            User32.SetWindowPos(handle, User32.HWND_NOTOPMOST, 0, 0, 0, 0,
                User32.SWP_NOMOVE | User32.SWP_NOSIZE | User32.SWP_NOACTIVATE);
    }

    public static void MoveWindow(this Window window,
        double pxLeft,
        double pxTop,
        double width,
        double height)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();

        // scale the size to the primary display
        TransformToPixels(window, width, height,
            out var pxWidth, out var pxHeight);

        // Use absolute location and relative size. WPF will scale the size to the target display
        User32.MoveWindow(handle, (int)Math.Round(pxLeft), (int)Math.Round(pxTop), pxWidth, pxHeight, true);
    }

    public static Rect GetWindowRectInPixel(this Window window)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();

        User32.GetWindowRect(handle, out User32.RECT nRect);

        return new Rect(new Point(nRect.Left, nRect.Top), new Point(nRect.Right, nRect.Bottom));
    }

    private static void TransformToPixels(this Visual visual,
        double unitX,
        double unitY,
        out int pixelX,
        out int pixelY)
    {
        Matrix matrix;
        var source = PresentationSource.FromVisual(visual);
        if (source != null)
            matrix = source.CompositionTarget.TransformToDevice;
        else
            using (var src = new HwndSource(new HwndSourceParameters()))
            {
                matrix = src.CompositionTarget.TransformToDevice;
            }

        pixelX = (int)Math.Round(matrix.M11 * unitX);
        pixelY = (int)Math.Round(matrix.M22 * unitY);
    }

    public static bool IsForegroundWindowBelongToSelf()
    {
        var hwnd = User32.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return false;

        User32.GetWindowThreadProcessId(hwnd, out var procId);
        return procId == Process.GetCurrentProcess().Id;
    }

    public static void SetNoactivate(this Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        User32.SetWindowLong(hwnd, User32.GWL_EXSTYLE,
            User32.GetWindowLong(hwnd, User32.GWL_EXSTYLE) |
            User32.WS_EX_NOACTIVATE);
    }

    public static void RemoveWindowControls(this Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        User32.SetWindowLong(hwnd, User32.GWL_STYLE,
            User32.GetWindowLong(hwnd, User32.GWL_STYLE) &
            ~User32.WS_SYSMENU);
    }

    public static void EnableBlur(Window window)
    {
        var accent = new AccentPolicy();
        var accentStructSize = Marshal.SizeOf(accent);
        accent.AccentState = AccentState.AccentEnableBlurbehind;
        accent.AccentFlags = 2;
        accent.GradientColor = 0x99FFFFFF;

        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
        Marshal.StructureToPtr(accent, accentPtr, false);

        var data = new WindowCompositionAttributeData
        {
            Attribute = WindowCompositionAttribute.WcaAccentPolicy,
            SizeOfData = accentStructSize,
            Data = accentPtr
        };

        User32.SetWindowCompositionAttribute(new WindowInteropHelper(window).Handle, ref data);

        Marshal.FreeHGlobal(accentPtr);
    }

    public static void EnableAcrylicBlur(Window window, Color tintColor, bool isDarkTheme, double tintOpacity = 0.7d)
    {
        window.Background = Brushes.Transparent;

        var hwnd = new WindowInteropHelper(window).EnsureHandle();

        if (!window.AllowsTransparency && HwndSource.FromHwnd(hwnd) is HwndSource hwndSource)
        {
            hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;
        }

        if (Environment.OSVersion.Version >= new Version(10, 0, 22000))
        {
            var captionColor = -2;
            Dwmapi.DwmSetWindowAttribute(hwnd, (uint)Dwmapi.WindowAttribute.CaptionColor, ref captionColor, Marshal.SizeOf(typeof(int)));
        }

        SetImmersiveDarkMode(hwnd, isDarkTheme);

        var margins = new Dwmapi.Margins(-1, -1, -1, -1);
        Dwmapi.DwmExtendFrameIntoClientArea(hwnd, ref margins);

        var accent = new AccentPolicy();
        var accentStructSize = Marshal.SizeOf(accent);
        accent.AccentState = AccentState.AccentEnableAcrylicblurbehind;
        accent.GradientColor = ToAbgr(tintColor, tintOpacity);

        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
        Marshal.StructureToPtr(accent, accentPtr, false);

        var data = new WindowCompositionAttributeData
        {
            Attribute = WindowCompositionAttribute.WcaAccentPolicy,
            SizeOfData = accentStructSize,
            Data = accentPtr
        };

        User32.SetWindowCompositionAttribute(hwnd, ref data);

        Marshal.FreeHGlobal(accentPtr);
    }

    public static void DisableDwmBlur(Window window)
    {
        var accent = new AccentPolicy();
        var accentStructSize = Marshal.SizeOf(accent);
        accent.AccentState = AccentState.AccentDisabled;

        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
        Marshal.StructureToPtr(accent, accentPtr, false);

        var data = new WindowCompositionAttributeData
        {
            Attribute = WindowCompositionAttribute.WcaAccentPolicy,
            SizeOfData = accentStructSize,
            Data = accentPtr
        };

        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        User32.SetWindowCompositionAttribute(hwnd, ref data);

        Marshal.FreeHGlobal(accentPtr);

        var margins = new Dwmapi.Margins(0, 0, 0, 0);
        Dwmapi.DwmExtendFrameIntoClientArea(hwnd, ref margins);

        if (Environment.OSVersion.Version >= new Version(10, 0, 21996))
        {
            var micaEnabled = 0;
            Dwmapi.DwmSetWindowAttribute(hwnd, (uint)Dwmapi.WindowAttribute.MicaEffect, ref micaEnabled, Marshal.SizeOf(typeof(int)));
        }

        if (Environment.OSVersion.Version >= new Version(10, 0, 22523))
        {
            var backdropType = (int)Dwmapi.SystembackdropType.None;
            Dwmapi.DwmSetWindowAttribute(hwnd, (uint)Dwmapi.WindowAttribute.SystembackdropType, ref backdropType, Marshal.SizeOf(typeof(int)));
        }

        if (Environment.OSVersion.Version >= new Version(10, 0, 22000))
        {
            var captionColor = -1;
            Dwmapi.DwmSetWindowAttribute(hwnd, (uint)Dwmapi.WindowAttribute.CaptionColor, ref captionColor, Marshal.SizeOf(typeof(int)));
        }

        SetImmersiveDarkMode(hwnd, false);

        // Restore system rounded corners
        if (Environment.OSVersion.Version >= new Version(10, 0, 22000))
        {
            int cornerPreference = (int)Dwmapi.WindowCornerStyle.Round;
            Dwmapi.DwmSetWindowAttribute(hwnd, (uint)Dwmapi.WindowAttribute.WindowCornerPreference, ref cornerPreference, Marshal.SizeOf(typeof(int)));
        }
    }

    private static void SetImmersiveDarkMode(nint hwnd, bool enabled)
    {
        if (hwnd == IntPtr.Zero || Environment.OSVersion.Version < new Version(10, 0, 17763))
            return;

        var darkMode = enabled ? 1 : 0;
        var attribute = Environment.OSVersion.Version < new Version(10, 0, 18985)
            ? (uint)Dwmapi.WindowAttribute.UseImmersiveDarkModeOld
            : (uint)Dwmapi.WindowAttribute.UseImmersiveDarkMode;

        Dwmapi.DwmSetWindowAttribute(hwnd, attribute, ref darkMode, Marshal.SizeOf(typeof(int)));
    }

    private static void EnableDwmBlur(Window window, bool isDarkTheme, uint dwAttribute, int pvAttribute)
    {
        // Mica will handle the color
        window.Background = Brushes.Transparent;

        var hwnd = new WindowInteropHelper(window).EnsureHandle();

        if (!window.AllowsTransparency && HwndSource.FromHwnd(hwnd) is HwndSource hwndSource)
        {
            hwndSource.CompositionTarget.BackgroundColor = Colors.Transparent;
        }

        if (Environment.OSVersion.Version >= new Version(10, 0, 22000))
        {
            var captionColor = -2;
            Dwmapi.DwmSetWindowAttribute(hwnd, (uint)Dwmapi.WindowAttribute.CaptionColor, ref captionColor, Marshal.SizeOf(typeof(int)));
        }

        var isDarkThemeInt = isDarkTheme ? 1 : 0;
        Dwmapi.DwmSetWindowAttribute(hwnd, (uint)Dwmapi.WindowAttribute.UseImmersiveDarkMode, ref isDarkThemeInt, Marshal.SizeOf(typeof(int)));

        var margins = new Dwmapi.Margins(-1, -1, -1, -1);
        Dwmapi.DwmExtendFrameIntoClientArea(hwnd, ref margins);

        var val = pvAttribute;
        Dwmapi.DwmSetWindowAttribute(hwnd, dwAttribute, ref val, Marshal.SizeOf(typeof(int)));
    }

    public static void EnableMicaBlur(Window window, bool isDarkTheme)
    {
        EnableDwmBlur(window, isDarkTheme, (uint)Dwmapi.WindowAttribute.MicaEffect, 1);
    }

    public static void EnableBackdropMicaBlur(Window window, bool isDarkTheme)
    {
        EnableDwmBlur(window, isDarkTheme, (uint)Dwmapi.WindowAttribute.SystembackdropType, (int)Dwmapi.SystembackdropType.Mica);
    }

    public static void EnableBackdropAcrylicBlur(Window window, bool isDarkTheme)
    {
        EnableDwmBlur(window, isDarkTheme, (uint)Dwmapi.WindowAttribute.SystembackdropType, (int)Dwmapi.SystembackdropType.Acrylic);
    }

    public static void EnableBackdropTabbedBlur(Window window, bool isDarkTheme)
    {
        EnableDwmBlur(window, isDarkTheme, (uint)Dwmapi.WindowAttribute.SystembackdropType, (int)Dwmapi.SystembackdropType.Tabbed);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public nint Data;
        public int SizeOfData;
    }

    private enum AccentState
    {
        AccentDisabled = 0,
        AccentEnableGradient = 1,
        AccentEnableTransparentgradient = 2,
        AccentEnableBlurbehind = 3,
        AccentEnableAcrylicblurbehind = 4,
        AccentInvalidState = 5,
    }

    private static uint ToAbgr(Color color, double alphaScale)
    {
        return (uint)(color.R << 0 |
            color.G << 8 |
            color.B << 16 |
            (int)(color.A * alphaScale) << 24);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public uint GradientColor;
        public readonly int AnimationId;
    }
}