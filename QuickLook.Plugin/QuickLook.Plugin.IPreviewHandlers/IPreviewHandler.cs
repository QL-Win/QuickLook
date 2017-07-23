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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QuickLook.Plugin.IPreviewHandlers
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("8895b1c6-b41f-4c1c-a562-0d564250836f")]
    internal interface IPreviewHandler
    {
        void SetWindow(IntPtr hwnd, ref Rectangle rect);
        void SetRect(ref Rectangle rect);
        void DoPreview();
        void Unload();
        void SetFocus();
        void QueryFocus(out IntPtr phwnd);

        [PreserveSig]
        uint TranslateAccelerator(ref Message pmsg);
    }
}