using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickLook.NativeMethods
{
    internal static class QuickLook
    {
        [DllImport("QuickLook.Native.Shell32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern FocusedWindowType GetFocusedWindowType();

        [DllImport("QuickLook.Native.Shell32.dll", EntryPoint = "GetCurrentSelection",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GetCurrentSelectionNative([MarshalAs(UnmanagedType.LPWStr)] StringBuilder sb);

        internal static string GetCurrentSelection()
        {
            StringBuilder sb = null;
            // communicate with COM in a separate thread
            Task.Run(() =>
            {
                sb = new StringBuilder(255 + 1);
                GetCurrentSelectionNative(sb);
            }).Wait();

            return sb?.ToString() ?? string.Empty;
        }

        internal enum FocusedWindowType
        {
            Invalid,
            Desktop,
            Explorer
        }
    }
}