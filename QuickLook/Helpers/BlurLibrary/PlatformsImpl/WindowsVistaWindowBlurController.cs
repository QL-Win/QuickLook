// Copyright © 2017 Paddy Xu
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
using System.Windows.Interop;

namespace QuickLook.Helpers.BlurLibrary.PlatformsImpl
{
    internal class WindowsVistaWindowBlurController : IWindowBlurController
    {
        public void EnableBlur(IntPtr hwnd)
        {
            if (!NativeThings.WindowsVistaAnd7.NativeMethods.DwmIsCompositionEnabled())
                return;

            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);

            InitializeGlass(hwnd);
            Enabled = true;
        }

        public void DisableBlur(IntPtr hwnd)
        {
            if (!NativeThings.WindowsVistaAnd7.NativeMethods.DwmIsCompositionEnabled())
                return;

            HwndSource.FromHwnd(hwnd)?.RemoveHook(WndProc);

            DeinitializeGlass(hwnd);
            Enabled = false;
        }

        public bool Enabled { get; private set; }

        public bool CanBeEnabled { get; } = true;

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != NativeThings.WindowsVistaAnd7.NativeMethods.WM_DWMCOMPOSITIONCHANGED)
                return IntPtr.Zero;

            InitializeGlass(hwnd);
            handled = false;

            return IntPtr.Zero;
        }

        private static void InitializeGlass(IntPtr hwnd)
        {
            // fill the background with glass
            var margins = new NativeThings.WindowsVistaAnd7.NativeMethods.MARGINS();
            margins.cxLeftWidth = margins.cxRightWidth = margins.cyBottomHeight = margins.cyTopHeight = -1;
            NativeThings.WindowsVistaAnd7.NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref margins);

            // initialize blur for the window
            var bbh = new NativeThings.WindowsVistaAnd7.NativeMethods.DWM_BLURBEHIND
            {
                fEnable = true,
                dwFlags = NativeThings.WindowsVistaAnd7.NativeMethods.DWM_BB.DWM_BB_ENABLE
            };

            NativeThings.WindowsVistaAnd7.NativeMethods.DwmEnableBlurBehindWindow(hwnd, ref bbh);
        }

        private static void DeinitializeGlass(IntPtr hwnd)
        {
            // fill the background with glass
            var margins = new NativeThings.WindowsVistaAnd7.NativeMethods.MARGINS();
            margins.cxLeftWidth = margins.cxRightWidth = margins.cyBottomHeight = margins.cyTopHeight = -1;
            NativeThings.WindowsVistaAnd7.NativeMethods.DwmExtendFrameIntoClientArea(hwnd, ref margins);

            // initialize blur for the window
            var bbh = new NativeThings.WindowsVistaAnd7.NativeMethods.DWM_BLURBEHIND
            {
                fEnable = false,
                dwFlags = NativeThings.WindowsVistaAnd7.NativeMethods.DWM_BB.DWM_BB_ENABLE
            };

            NativeThings.WindowsVistaAnd7.NativeMethods.DwmEnableBlurBehindWindow(hwnd, ref bbh);
        }
    }
}