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
using System.Drawing;
using System.Runtime.InteropServices;

namespace QuickLook.Common.Helpers
{
    public static class DpiHelper
    {
        public const float DefaultDpi = 96;

        public static ScaleFactor GetCurrentScaleFactor()
        {
            var g = Graphics.FromHwnd(IntPtr.Zero);
            var desktop = g.GetHdc();

            var horizontalDpi = GetDeviceCaps(desktop, (int) DeviceCap.LOGPIXELSX);
            var verticalDpi = GetDeviceCaps(desktop, (int) DeviceCap.LOGPIXELSY);

            return new ScaleFactor {Horizontal = horizontalDpi / DefaultDpi, Vertical = verticalDpi / DefaultDpi};
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern int GetDeviceCaps(IntPtr hDC, int nIndex);

        private enum DeviceCap
        {
            /// <summary>
            ///     Logical pixels inch in X
            /// </summary>
            LOGPIXELSX = 88,
            /// <summary>
            ///     Logical pixels inch in Y
            /// </summary>
            LOGPIXELSY = 90
        }

        public struct ScaleFactor
        {
            public float Horizontal;
            public float Vertical;
        }
    }
}