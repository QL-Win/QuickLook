using System;
using System.Windows;
using System.Windows.Interop;

namespace QuickLook.Helpers.BlurLibrary
{
    public static class BlurWindow
    {
        private static readonly IWindowBlurController BlurController;

        static BlurWindow()
        {
            BlurController = Helpers.GetWindowControllerForOs(OsHelper.GetOsType());
        }

        /// <summary>
        ///     Current blur state
        /// </summary>
        public static bool Enabled => BlurController.Enabled;

        /// <summary>
        ///     Checks if blur can be enabled.
        /// </summary>
        public static bool CanBeEnabled => BlurController.CanBeEnabled;

        private static void EnableWindowBlur(IntPtr hwnd)
        {
            if (!CanBeEnabled)
                return;

            BlurController.EnableBlur(hwnd);
        }

        /// <summary>
        ///     Enable blur for window
        /// </summary>
        /// <param name="window">Window object</param>
        public static void EnableWindowBlur(Window window)
        {
            EnableWindowBlur(new WindowInteropHelper(window).Handle);
        }

        private static void DisableWindowBlur(IntPtr hwnd)
        {
            if (!CanBeEnabled)
                return;

            BlurController.DisableBlur(hwnd);
        }

        /// <summary>
        ///     Disable blur for window
        /// </summary>
        /// <param name="window">Window object</param>
        public static void DisableWindowBlur(Window window)
        {
            DisableWindowBlur(new WindowInteropHelper(window).Handle);
        }
    }
}