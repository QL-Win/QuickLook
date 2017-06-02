using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickLook.NativeMethods
{
    internal static class QuickLook
    {
        [DllImport("QuickLook.Native.Shell32.dll", EntryPoint = "GetFocusedWindowType",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern FocusedWindowType GetFocusedWindowTypeNative_32();

        [DllImport("QuickLook.Native.Shell32.dll", EntryPoint = "GetCurrentSelection",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GetCurrentSelectionNative_32([MarshalAs(UnmanagedType.LPWStr)] StringBuilder sb);

        [DllImport("QuickLook.Native.Shell32.x64.dll", EntryPoint = "GetFocusedWindowType",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern FocusedWindowType GetFocusedWindowTypeNative_64();

        [DllImport("QuickLook.Native.Shell32.x64.dll", EntryPoint = "GetCurrentSelection",
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GetCurrentSelectionNative_64([MarshalAs(UnmanagedType.LPWStr)] StringBuilder sb);

        internal static FocusedWindowType GetFocusedWindowType()
        {
            return App.Is64Bit ? GetFocusedWindowTypeNative_64() : GetFocusedWindowTypeNative_32();
        }

        internal static string GetCurrentSelection()
        {
            StringBuilder sb = null;
            // communicate with COM in a separate thread
            Task.Run(() =>
            {
                sb = new StringBuilder(255 + 1);
                if (App.Is64Bit)
                    GetCurrentSelectionNative_64(sb);
                else
                    GetCurrentSelectionNative_32(sb);
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