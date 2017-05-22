using System;
using System.Diagnostics;
using System.Text;
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

        internal static void SetNoactivate(WindowInteropHelper window)
        {
            User32.SetWindowLong(window.Handle, User32.GWL_EXSTYLE,
                User32.GetWindowLong(window.Handle, User32.GWL_EXSTYLE) |
                User32.WS_EX_NOACTIVATE);
        }

        internal static string GetWindowClassName(IntPtr window)
        {
            var pClassName = new StringBuilder(256);
            User32.GetClassName(window, pClassName, pClassName.Capacity);

            return pClassName.ToString();
        }

        internal static IntPtr GetParentWindow(IntPtr child)
        {
            return User32.GetParent(child);
        }

        internal static IntPtr GetFocusedWindow()
        {
            var activeWindowHandle = User32.GetForegroundWindow();

            var activeWindowThread = User32.GetWindowThreadProcessId(activeWindowHandle, IntPtr.Zero);
            var currentThread = Kernel32.GetCurrentThreadId();

            User32.AttachThreadInput(activeWindowThread, currentThread, true);
            var focusedControlHandle = User32.GetFocus();
            User32.AttachThreadInput(activeWindowThread, currentThread, false);

            return focusedControlHandle;
        }

        internal static bool IsFocusedWindowSelf()
        {
            var procId = Process.GetCurrentProcess().Id;
            uint activeProcId;
            User32.GetWindowThreadProcessId(GetFocusedWindow(), out activeProcId);

            return activeProcId == procId;
        }

        internal static bool IsFocusedControlExplorerItem()
        {
            if (NativeMethods.QuickLook.GetFocusedWindowType() == 0)
                return false;

            var focusedWindowClass = GetWindowClassName(GetFocusedWindow());
            var focusedWindowParentClass =
                GetWindowClassName(GetParentWindow(GetFocusedWindow()));

            if (focusedWindowClass != "SysListView32" && focusedWindowClass != "DirectUIHWND")
                return false;

            if (focusedWindowParentClass != "SHELLDLL_DefView")
                return false;

            return true;
        }
    }
}