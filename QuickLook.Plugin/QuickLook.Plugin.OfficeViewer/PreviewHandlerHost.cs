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
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QuickLook.Plugin.OfficeViewer;

/// <summary>
/// A Windows Forms host for Preview Handlers.
/// Supports out-of-process COM isolation (prevhost.exe via DllSurrogate) with automatic
/// in-process fallback, and tries initialization via IInitializeWithStream →
/// IInitializeWithItem → IInitializeWithFile in priority order — mirroring PowerToys Peek.
/// </summary>
public class PreviewHandlerHost : Control
{
    // IID constants
    private static readonly Guid IidIClassFactory = new("00000001-0000-0000-C000-000000000046");

    private static readonly Guid IidIUnknown = new("00000000-0000-0000-C000-000000000046");
    private static readonly Guid IidIShellItem = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");

    // CLSCTX_LOCAL_SERVER — COM creates the server out-of-process (prevhost.exe when
    // a DllSurrogate is registered for the CLSID's AppID, which most Office/shell handlers have).
    private const uint ClsctxLocalServer = 0x4;

    // Cache class factories so that the out-of-proc host (prevhost.exe) stays alive and
    // subsequent previews load faster — same technique used by PowerToys Peek.
    private static readonly ConcurrentDictionary<Guid, IClassFactory> HandlerFactories = new();

    [DllImport("ole32.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern int CoGetClassObject(
        ref Guid rclsid,
        uint dwClsContext,
        IntPtr pvReserved,
        ref Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = true)]
    private static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    private IPreviewHandler _mCurrentPreviewHandler;
    private Stream _fileStream;

    /// <summary>
    /// Initialize a new instance of the PreviewHandlerHost class.
    /// </summary>
    public PreviewHandlerHost()
    {
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
    protected override void Dispose(bool disposing)
    {
        UnloadPreviewHandler();

        _fileStream?.Dispose();
        _fileStream = null;

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
    /// Opens the specified file using the appropriate preview handler and displays the result
    /// in this PreviewHandlerHost. Initialization priority: stream → item → file path.
    /// </summary>
    public bool Open(string path)
    {
        UnloadPreviewHandler();
        _fileStream?.Dispose();
        _fileStream = null;

        if (string.IsNullOrEmpty(path))
            return false;

        var guid = ShellExRegister.GetPreviewHandlerGUID(Path.GetExtension(path));
        if (guid == Guid.Empty)
            return false;

        CurrentPreviewHandler = guid;

        // 1. Try out-of-process COM first (prevhost.exe via DllSurrogate).
        //    If the handler is not registered for surrogate hosting, fall back to in-process.
        var o = TryCreateOutOfProcHandler(guid) ?? TryCreateInProcHandler(guid);
        if (o == null)
            return false;

        // 2. Initialize via the best-available interface: stream → item → file.
        const uint stgmRead = 0;
        bool initialized = false;

        if (!initialized && o is IInitializeWithStream streamInit)
        {
            try
            {
                _fileStream = File.OpenRead(path);
                streamInit.Initialize(new IStreamWrapper(_fileStream), stgmRead);
                initialized = true;
            }
            catch
            {
                _fileStream?.Dispose();
                _fileStream = null;
            }
        }

        if (!initialized && o is IInitializeWithItem itemInit)
        {
            try
            {
                var riid = IidIShellItem;
                int hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref riid, out var shellItemObj);
                if (hr >= 0)
                {
                    itemInit.Initialize((IShellItem)shellItemObj, stgmRead);
                    initialized = true;
                }
            }
            catch { }
        }

        if (!initialized && o is IInitializeWithFile fileInit)
        {
            try
            {
                fileInit.Initialize(path, stgmRead);
                initialized = true;
            }
            catch { }
        }

        if (!initialized)
        {
            Marshal.FinalReleaseComObject(o);
            return false;
        }

        _mCurrentPreviewHandler = o as IPreviewHandler;
        if (_mCurrentPreviewHandler == null)
        {
            Marshal.FinalReleaseComObject(o);
            return false;
        }

        if (IsDisposed)
            return false;

        var r = ClientRectangle;
        _mCurrentPreviewHandler.SetWindow(Handle, ref r);
        _mCurrentPreviewHandler.DoPreview();

        return true;
    }

    /// <summary>
    /// Unloads the preview handler hosted in this PreviewHandlerHost.
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

    /// <summary>
    /// Attempts to create the COM server out-of-process via a cached IClassFactory.
    /// Returns null if the CLSID has no out-of-process (surrogate) registration.
    /// Retries once if the host process appears to have died between calls.
    /// </summary>
    private static object TryCreateOutOfProcHandler(Guid clsid)
    {
        bool retry = false;
        do
        {
            if (!HandlerFactories.TryGetValue(clsid, out var factory))
            {
                var factoryIid = IidIClassFactory;
                int hr = CoGetClassObject(ref clsid, ClsctxLocalServer, IntPtr.Zero, ref factoryIid, out var factoryObj);
                if (hr < 0)
                    return null; // No local-server / surrogate registration — use in-proc.

                factory = (IClassFactory)factoryObj;
                factory.LockServer(true);
                HandlerFactories.TryAdd(clsid, factory);
            }

            try
            {
                var iid = IidIUnknown;
                factory.CreateInstance(null, ref iid, out var instance);
                return instance;
            }
            catch (COMException)
            {
                if (!retry)
                {
                    // Host process likely died; evict stale factory and retry once.
                    HandlerFactories.TryRemove(clsid, out _);
                    retry = true;
                }
                else
                {
                    break;
                }
            }
        }
        while (retry);

        return null;
    }

    /// <summary>
    /// Falls back to in-process COM instantiation via <see cref="Activator.CreateInstance"/>.
    /// </summary>
    private static object TryCreateInProcHandler(Guid clsid)
    {
        try
        {
            return Activator.CreateInstance(Type.GetTypeFromCLSID(clsid, true));
        }
        catch
        {
            return null;
        }
    }
}
