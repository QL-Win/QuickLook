using System;
using System.Runtime.InteropServices;
using QuickLook.Helpers.BlurLibrary.NativeThings.Windows10;

namespace QuickLook.Helpers.BlurLibrary.PlatformsImpl
{
    internal class Windows10WindowBlurController : IWindowBlurController
    {
        public void EnableBlur(IntPtr hwnd)
        {
            var accent = new AccentPolicy {AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND};

            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            NativeThings.Windows10.NativeMethods.SetWindowCompositionAttribute(hwnd, ref data);

            Marshal.FreeHGlobal(accentPtr);

            Enabled = true;
        }

        public void DisableBlur(IntPtr hwnd)
        {
            var accent = new AccentPolicy {AccentState = AccentState.ACCENT_DISABLED};

            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            NativeThings.Windows10.NativeMethods.SetWindowCompositionAttribute(hwnd, ref data);

            Marshal.FreeHGlobal(accentPtr);

            Enabled = false;
        }

        public bool Enabled { get; private set; }
        public bool CanBeEnabled { get; } = true;
    }
}