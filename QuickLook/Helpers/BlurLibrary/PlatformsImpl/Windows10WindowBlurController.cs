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