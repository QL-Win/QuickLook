// Copyright © 2017 Paddy Xu
// 
// This file is part of QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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