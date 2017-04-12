using System.Runtime.InteropServices;
using System.Text;

namespace QuickLook.NativeMethods
{
    internal class QuickLook
    {
        [DllImport("QuickLook.Native.Shell32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SaveCurrentSelection();

        [DllImport("QuickLook.Native.Shell32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetCurrentSelectionCount();

        [DllImport("QuickLook.Native.Shell32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GetCurrentSelectionBuffer([MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer);
    }
}