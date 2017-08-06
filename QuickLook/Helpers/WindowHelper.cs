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
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using QuickLook.NativeMethods;

namespace QuickLook.Helpers
{
    public static class WindowHelper
    {
        public static Rect GetCurrentWindowRect()
        {
            var screen = Screen.FromPoint(Cursor.Position).WorkingArea;
            var scale = DpiHelper.GetCurrentScaleFactor();
            return new Rect(
                new Point(screen.X / scale.Horizontal, screen.Y / scale.Vertical),
                new Size(screen.Width / scale.Horizontal, screen.Height / scale.Vertical));
        }

        public static void BringToFront(this Window window)
        {
            var handle = new WindowInteropHelper(window).Handle;
            User32.SetWindowPos(handle, User32.HWND_TOPMOST, 0, 0, 0, 0,
                User32.SWP_NOMOVE | User32.SWP_NOSIZE | User32.SWP_NOACTIVATE);
            User32.SetWindowPos(handle, User32.HWND_NOTOPMOST, 0, 0, 0, 0,
                User32.SWP_NOMOVE | User32.SWP_NOSIZE | User32.SWP_NOACTIVATE);
        }

        public static void MoveWindow(this Window window,
            double left,
            double top,
            double width,
            double height)
        {
            int pxLeft = 0, pxTop = 0;
            if (left != 0 || top != 0)
                window.TransformToPixels(left, top,
                    out pxLeft, out pxTop);

            int pxWidth, pxHeight;
            window.TransformToPixels(width, height,
                out pxWidth, out pxHeight);

            User32.MoveWindow(new WindowInteropHelper(window).Handle, pxLeft, pxTop, pxWidth, pxHeight, true);
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

            pixelX = (int) (matrix.M11 * unitX);
            pixelY = (int) (matrix.M22 * unitY);
        }

        internal static bool IsForegroundWindowBelongToSelf()
        {
            var hwnd = User32.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
                return false;

            User32.GetWindowThreadProcessId(hwnd, out uint procId);
            return procId == Process.GetCurrentProcess().Id;
        }

        internal static void SetNoactivate(WindowInteropHelper window)
        {
            User32.SetWindowLong(window.Handle, User32.GWL_EXSTYLE,
                User32.GetWindowLong(window.Handle, User32.GWL_EXSTYLE) |
                User32.WS_EX_NOACTIVATE);
        }
    }
}