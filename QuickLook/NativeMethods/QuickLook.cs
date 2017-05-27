using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickLook.NativeMethods
{
    internal static class QuickLook
    {
        [DllImport("QuickLook.Native.Shell32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetFocusedWindowType();

        [DllImport("QuickLook.Native.Shell32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SaveCurrentSelection();

        [DllImport("QuickLook.Native.Shell32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetCurrentSelectionCount();

        [DllImport("QuickLook.Native.Shell32.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void GetCurrentSelectionBuffer([MarshalAs(UnmanagedType.LPWStr)] StringBuilder buffer);

        internal static string[] GetCurrentSelection()
        {
            StringBuilder sb = null;
            // communicate with COM in a separate thread
            Task.Run(() =>
            {
                SaveCurrentSelection();

                var n = GetCurrentSelectionCount();
                if (n != 0)
                {
                    sb = new StringBuilder(n * 261); // MAX_PATH + NULL = 261
                    GetCurrentSelectionBuffer(sb);
                }
            }).Wait();
            return sb == null || sb.Length == 0 ? new string[0] : sb.ToString().Split('|');
        }

        internal static string GetCurrentSelectionFirst()
        {
            var files = GetCurrentSelection();

            return files.Any() ? files.First() : string.Empty;
        }
    }
}