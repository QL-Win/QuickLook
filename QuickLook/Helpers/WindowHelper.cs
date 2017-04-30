using System;
using System.Text;
using System.Windows.Interop;
using QuickLook.NativeMethods;

namespace QuickLook.Helpers
{
    internal static class WindowHelper
    {
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

        internal static bool IsFocusedControlExplorerItem()
        {
            if (NativeMethods.QuickLook.GetFocusedWindowType() == 0)
                return false;

            var focusedWindowClass = GetWindowClassName(GetFocusedWindow());
            var focusedWindowParentClass =
                GetWindowClassName(GetParentWindow(GetFocusedWindow()));

            if (focusedWindowParentClass != "SHELLDLL_DefView")
                return false;

            if (focusedWindowClass != "SysListView32" && focusedWindowClass != "DirectUIHWND")
                return false;

            return true;
        }
    }
}