using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace

namespace QuickLook.Helpers.BlurLibrary.NativeThings.Windows10
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
    }
}