using System;
using System.Runtime.InteropServices;

namespace QuickLook.NativeMethods
{
    internal class Kernel32
    {
        [DllImport("kernel32.dll")]
        internal static extern IntPtr LoadLibrary(string lpFileName);
    }
}