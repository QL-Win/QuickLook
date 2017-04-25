using System.Windows.Interop;
using QuickLook.NativeMethods;

namespace QuickLook.Utilities
{
    internal class NoactivateWindowHelper
    {
        internal static void SetNoactivate(WindowInteropHelper window)
        {
            User32.SetWindowLong(window.Handle, User32.GWL_EXSTYLE,
                User32.GetWindowLong(window.Handle, User32.GWL_EXSTYLE) |
                User32.WS_EX_NOACTIVATE);
        }
    }
}