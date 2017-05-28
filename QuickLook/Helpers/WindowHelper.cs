using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using QuickLook.NativeMethods;

namespace QuickLook.Helpers
{
    internal static class WindowHelper
    {
        public static Rect GetCurrentWindowRect()
        {
            var screen = Screen.FromPoint(Cursor.Position).WorkingArea;
            var dpi = DpiHelper.GetCurrentDpi();
            var scaleX = dpi.HorizontalDpi / DpiHelper.DEFAULT_DPI;
            var scaleY = dpi.VerticalDpi / DpiHelper.DEFAULT_DPI;
            return new Rect(
                new Point(screen.X / scaleX, screen.Y / scaleY),
                new Size(screen.Width / scaleX, screen.Height / scaleY));
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

            var helper = new WindowInteropHelper(window);
            User32.MoveWindow(helper.Handle, pxLeft, pxTop, pxWidth, pxHeight, true);
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