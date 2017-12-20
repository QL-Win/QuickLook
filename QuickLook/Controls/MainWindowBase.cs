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
using System.Windows;
using System.Windows.Interop;
using QuickLook.Helpers;

namespace QuickLook.Controls
{
    public class MainWindowBase : Window
    {
        public MainWindowBase()
        {
            //WindowStyle = WindowStyle.None;

            SourceInitialized += SourceInitializedHandler;
        }

        private void SourceInitializedHandler(object sender, EventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            var handleSource = HwndSource.FromHwnd(handle);
            handleSource?.AddHook(WindowProc);
        }

        private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024: /* WM_GETMINMAXINFO */
                    WindowHelper.WmGetMinMaxInfo(hwnd, lParam);
                    //handled = true;
                    break;
            }

            return (IntPtr) 0;
        }
    }
}