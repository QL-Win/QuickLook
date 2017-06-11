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
using QuickLook.Helpers.BlurLibrary.PlatformsImpl;

namespace QuickLook.Helpers.BlurLibrary
{
    internal static class Helpers
    {
        internal static IWindowBlurController GetWindowControllerForOs(OsType osType)
        {
            switch (osType)
            {
                case OsType.WindowsVista:
                    return new WindowsVistaWindowBlurController();
                case OsType.Windows7:
                    return new Windows7WindowBlurController();
                case OsType.Windows8:
                    return new Windows8WindowBlurController();
                case OsType.Windows81:
                    return new Windows81WindowBlurController();
                case OsType.Windows10:
                    return new Windows10WindowBlurController();
                case OsType.Other:
                    return new OsNotSupportedWindowBlurController();
                default:
                    return new OsNotSupportedWindowBlurController();
            }
        }

        internal static IntPtr GetWindowHandle(Window window)
        {
            return new WindowInteropHelper(window).Handle;
        }
    }
}