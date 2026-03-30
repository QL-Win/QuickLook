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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms.Integration;

namespace QuickLook.Plugin.OfficeViewer;

public class PreviewPanel : WindowsFormsHost, IDisposable
{
    private PreviewHandlerHost _control;
    private CancellationTokenSource _cts;

    public new void Dispose()
    {
        // Cancel any in-progress background load before disposing
        _cts?.Cancel();

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

        // Capture the HWND and client rect on the UI thread before going async,
        // so the background thread never needs to access thread-affine WinForms properties.
        var hwnd = _control.Handle;
        var rect = _control.ClientRectangle;

        var cts = new CancellationTokenSource();
        _cts = cts;

        // Perform the blocking COM preview-handler calls on a dedicated STA background
        // thread so the WPF UI thread remains responsive during the (potentially slow)
        // first-time load of the Office preview handler.
        var thread = new Thread(() =>
        {
            try
            {
                if (!cts.Token.IsCancellationRequested)
                    _control.OpenBackground(file, hwnd, rect);
            }
            catch (Exception e)
            {
                // Log COM/preview-handler errors for diagnostic purposes.
                // These are non-fatal: the preview simply won't appear.
                Debug.WriteLine($"[OfficeViewer] Preview failed: {e.Message}");
            }
            finally
            {
                if (!cts.Token.IsCancellationRequested)
                    context.IsBusy = false;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetActiveWindow(IntPtr hWnd);
}
