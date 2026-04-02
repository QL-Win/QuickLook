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

namespace QuickLook.Common.NativeMethods;

public static class Dwmapi
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Margins(int cxLeftWidth, int cxRightWidth, int cyTopHeight, int cyBottomHeight)
    {
        public int cxLeftWidth = cxLeftWidth;
        public int cxRightWidth = cxRightWidth;
        public int cyTopHeight = cyTopHeight;
        public int cyBottomHeight = cyBottomHeight;
    }

    public enum WindowAttribute
    {
        UseImmersiveDarkModeOld = 19,
        UseImmersiveDarkMode = 20,
        WindowCornerPreference = 33,
        CaptionColor = 35,
        SystembackdropType = 38,
        MicaEffect = 1029,
    }

    public enum SystembackdropType
    {
        Auto = 0,
        None = 1,
        Mica = 2,
        Acrylic = 3, // Automatically selects the best Acrylic effect available on the system (Acrylic11 > Acrylic10)
        Tabbed = 4,
        Acrylic10 = 5, // Windows 10 style, supported on Windows 10 and 11
        Acrylic11 = 6, // Windows 11 style, supported on Windows 11 22523+ (Insider) and 22621+ (Stable)
    }

    public enum WindowCornerStyle : uint
    {
        /// <summary>
        /// Let the system decide whether or not to round window corners.
        /// Equivalent to DWMWCP_DEFAULT
        /// </summary>
        Default = 0,

        /// <summary>
        /// Never round window corners.
        /// Equivalent to DWMWCP_DONOTROUND
        /// </summary>
        DoNotRound = 1,

        /// <summary>
        /// Round the corners if appropriate.
        /// Equivalent to DWMWCP_ROUND
        /// </summary>
        Round = 2,

        /// <summary>
        /// Round the corners if appropriate, with a small radius.
        /// Equivalent to DWMWCP_ROUNDSMALL
        /// </summary>
        RoundSmall = 3,
    }

    [DllImport("DwmApi.dll")]
    public static extern int DwmExtendFrameIntoClientArea(nint hwnd, ref Margins pMarInset);

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(nint hwnd, uint dwAttribute, ref int pvAttribute, int cbAttribute);
}
