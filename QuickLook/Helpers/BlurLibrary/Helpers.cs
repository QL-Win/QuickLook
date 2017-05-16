using System;
using System.Windows;
using System.Windows.Interop;
using QuickLook.Helpers.BlurLibrary.PlatformsImpl;

namespace QuickLook.Helpers.BlurLibrary
{
    internal static class Helpers
    {
        internal static IWindowBlurController GetWindowControllerForOs(OsType osType)
        {
            switch (osType)
            {
                case OsType.WindowsVista:
                    return new WindowsVistaWindowBlurController();
                case OsType.Windows7:
                    return new Windows7WindowBlurController();
                case OsType.Windows8:
                    return new Windows8WindowBlurController();
                case OsType.Windows81:
                    return new Windows81WindowBlurController();
                case OsType.Windows10:
                    return new Windows10WindowBlurController();
                case OsType.Other:
                    return new OsNotSupportedWindowBlurController();
                default:
                    return new OsNotSupportedWindowBlurController();
            }
        }

        internal static IntPtr GetWindowHandle(Window window)
        {
            return new WindowInteropHelper(window).Handle;
        }
    }
}