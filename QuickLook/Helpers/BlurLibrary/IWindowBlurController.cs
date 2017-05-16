using System;

namespace QuickLook.Helpers.BlurLibrary
{
    internal interface IWindowBlurController
    {
        /// <summary>
        ///     Current blur state
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        ///     Checks if blur can be enabled.
        /// </summary>
        bool CanBeEnabled { get; }

        /// <summary>
        ///     Enable blur for window
        /// </summary>
        /// <param name="hwnd">Pointer to Window</param>
        /// <exception cref="NotImplementedException">Throws when blur is not supported.</exception>
        void EnableBlur(IntPtr hwnd);

        /// <summary>
        ///     Disable blur for window
        /// </summary>
        /// <param name="hwnd">Pointer to Window</param>
        /// <exception cref="NotImplementedException">Throws when blur is not supported.</exception>
        void DisableBlur(IntPtr hwnd);
    }
}