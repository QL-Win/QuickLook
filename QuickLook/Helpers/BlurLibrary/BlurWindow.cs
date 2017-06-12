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

namespace QuickLook.Helpers.BlurLibrary
{
    public static class BlurWindow
    {
        private static readonly IWindowBlurController BlurController;

        static BlurWindow()
        {
            BlurController = Helpers.GetWindowControllerForOs(OsHelper.GetOsType());
        }

        /// <summary>
        ///     Current blur state
        /// </summary>
        public static bool Enabled => BlurController.Enabled;

        /// <summary>
        ///     Checks if blur can be enabled.
        /// </summary>
        public static bool CanBeEnabled => BlurController.CanBeEnabled;

        private static void EnableWindowBlur(IntPtr hwnd)
        {
            if (!CanBeEnabled)
                return;

            BlurController.EnableBlur(hwnd);
        }

        /// <summary>
        ///     Enable blur for window
        /// </summary>
        /// <param name="window">Window object</param>
        public static void EnableWindowBlur(Window window)
        {
            EnableWindowBlur(new WindowInteropHelper(window).Handle);
        }

        private static void DisableWindowBlur(IntPtr hwnd)
        {
            if (!CanBeEnabled)
                return;

            BlurController.DisableBlur(hwnd);
        }

        /// <summary>
        ///     Disable blur for window
        /// </summary>
        /// <param name="window">Window object</param>
        public static void DisableWindowBlur(Window window)
        {
            DisableWindowBlur(new WindowInteropHelper(window).Handle);
        }
    }
}