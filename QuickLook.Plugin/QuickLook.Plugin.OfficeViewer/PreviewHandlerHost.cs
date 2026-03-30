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

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QuickLook.Plugin.OfficeViewer;

/// <summary>
/// A Windows Forms host for Preview Handlers.
/// </summary>
public class PreviewHandlerHost : Control
{
    /// <summary>
    /// The GUID for the IShellItem interface.
    /// </summary>
    internal const string GuidIshellitem = "43826d1e-e718-42ee-bc55-a1e261c37bfe";

    private IPreviewHandler _mCurrentPreviewHandler;

    /// <summary>
    /// Initialize a new instance of the PreviewHandlerHost class.
    /// </summary>
    public PreviewHandlerHost()
    {
        Size = new Size(320, 240);
    }

    /// <summary>
    /// Gets the GUID of the current preview handler.
    /// </summary>
    [Browsable(false)]
    [ReadOnly(true)]
    public Guid CurrentPreviewHandler { get; private set; } = Guid.Empty;

    /// <summary>
    /// Releases the unmanaged resources used by the PreviewHandlerHost and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        UnloadPreviewHandler();

        if (_mCurrentPreviewHandler != null)
        {
            Marshal.FinalReleaseComObject(_mCurrentPreviewHandler);
            _mCurrentPreviewHandler = null;
            GC.Collect();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Resizes the hosted preview handler when this PreviewHandlerHost is resized.
    /// </summary>
    /// <param name="e"></param>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        try
        {
            var r = ClientRectangle;
            _mCurrentPreviewHandler?.SetRect(ref r);
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x8001010D))
        {
            // RPC_E_CANTCALLOUT_ININPUTSYNCCALL
            // This exception occurs when an outgoing call cannot be made because 
            // the application is dispatching an input-synchronous call.
            // It's safe to ignore this exception as the preview handler will be 
            // resized on the next resize event.
        }
    }

    /// <summary>
    /// Opens the specified file using the appropriate preview handler and displays the result in this PreviewHandlerHost.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool Open(string path)
    {
        UnloadPreviewHandler();

        if (string.IsNullOrEmpty(path))
            return false;

        // try to get GUID for the preview handler
        var guid = ShellExRegister.GetPreviewHandlerGUID(Path.GetExtension(path));

        if (guid == Guid.Empty)
            return false;

        CurrentPreviewHandler = guid;
        var o = Activator.CreateInstance(Type.GetTypeFromCLSID(CurrentPreviewHandler, true));

        if (o is not IInitializeWithFile fileInit)
            return false;

        fileInit.Initialize(path, 0);
        _mCurrentPreviewHandler = o as IPreviewHandler;
        if (_mCurrentPreviewHandler == null)
            return false;

        if (IsDisposed)
            return false;

        // bind the preview handler to the control's bounds and preview the content
        var r = ClientRectangle;
        _mCurrentPreviewHandler.SetWindow(Handle, ref r);
        _mCurrentPreviewHandler.DoPreview();

        return true;
    }

    /// <summary>
    /// Opens the specified file using the appropriate preview handler.
    /// This overload accepts a pre-captured parent window handle and client rectangle,
    /// allowing it to be safely called from a background thread without accessing
    /// thread-affine WinForms control properties.
    /// </summary>
    /// <param name="path">The file path to preview.</param>
    /// <param name="parentHandle">Pre-captured HWND of the host control.</param>
    /// <param name="clientRect">Pre-captured client rectangle of the host control.</param>
    /// <returns>True if preview was successfully started; otherwise false.</returns>
    internal bool OpenBackground(string path, IntPtr parentHandle, Rectangle clientRect)
    {
        UnloadPreviewHandler();

        if (string.IsNullOrEmpty(path))
            return false;

        var guid = ShellExRegister.GetPreviewHandlerGUID(Path.GetExtension(path));

        if (guid == Guid.Empty)
            return false;

        var o = Activator.CreateInstance(Type.GetTypeFromCLSID(guid, true));

        if (o is not IInitializeWithFile fileInit)
            return false;

        fileInit.Initialize(path, 0);
        _mCurrentPreviewHandler = o as IPreviewHandler;
        if (_mCurrentPreviewHandler == null)
            return false;

        // Record the active handler GUID only after successful initialization
        // so OnResize never observes a partially-initialized state.
        CurrentPreviewHandler = guid;

        if (IsDisposed)
            return false;

        var r = clientRect;
        _mCurrentPreviewHandler.SetWindow(parentHandle, ref r);
        _mCurrentPreviewHandler.DoPreview();

        return true;
    }

    /// <summary>
    /// Unloads the preview handler hosted in this PreviewHandlerHost and closes the file stream.
    /// </summary>
    public void UnloadPreviewHandler()
    {
        try
        {
            _mCurrentPreviewHandler?.Unload();
        }
        catch (Exception)
        {
            // ignored
        }
    }
}
