using System;
using System.Runtime.InteropServices;

namespace QuickLook.NativeMethods
{
    internal static class Kernel32
    {
        [DllImport("kernel32.dll")]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetCurrentThreadId();
    }
}