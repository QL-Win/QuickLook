// Copyright © 2017-2026 QL-Win Contributors
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

using QuickLook.Common.Plugin;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms.Integration;

namespace QuickLook.Plugin.OfficeViewer;

public class PreviewPanel : WindowsFormsHost, IDisposable
{
    private PreviewHandlerHost _control;

    public new void Dispose()
    {
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            Child = null;
            _control?.Dispose();
            _control = null;

            base.Dispose();
        }));
    }

    public void PreviewFile(string file, ContextObject context)
    {
        _control = new PreviewHandlerHost();
        Child = _control;
        _control.Open(file);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetActiveWindow(IntPtr hWnd);
}
