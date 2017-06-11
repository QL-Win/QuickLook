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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace QuickLook.Plugin.IPreviewHandlers
{
    /// <summary>
    ///     A Windows Forms host for Preview Handlers.
    /// </summary>
    public class PreviewHandlerHost : Control
    {
        /// <summary>
        ///     The GUID for the IShellItem interface.
        /// </summary>
        internal const string GuidIshellitem = "43826d1e-e718-42ee-bc55-a1e261c37bfe";

        private IPreviewHandler _mCurrentPreviewHandler;

        /// <summary>
        ///     Initialialises a new instance of the PreviewHandlerHost class.
        /// </summary>
        public PreviewHandlerHost()
        {
            Size = new Size(320, 240);
        }

        /// <summary>
        ///     Gets the GUID of the current preview handler.
        /// </summary>
        [Browsable(false)]
        [ReadOnly(true)]
        public Guid CurrentPreviewHandler { get; private set; } = Guid.Empty;

        /// <summary>
        ///     Releases the unmanaged resources used by the PreviewHandlerHost and optionally releases the managed resources.
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
        ///     Returns the GUID of the preview handler associated with the specified file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static Guid GetPreviewHandlerGUID(string filename)
        {
            // open the registry key corresponding to the file extension
            var ext = Registry.ClassesRoot.OpenSubKey(Path.GetExtension(filename));
            if (ext != null)
            {
                // open the key that indicates the GUID of the preview handler type
                var test = ext.OpenSubKey("shellex\\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
                if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));

                // sometimes preview handlers are declared on key for the class
                var className = Convert.ToString(ext.GetValue(null));
                if (className != null)
                {
                    test = Registry.ClassesRoot.OpenSubKey(
                        className + "\\shellex\\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
                    if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));
                }
            }

            return Guid.Empty;
        }

        /// <summary>
        ///     Resizes the hosted preview handler when this PreviewHandlerHost is resized.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            var r = ClientRectangle;
            _mCurrentPreviewHandler?.SetRect(ref r);
        }

        /// <summary>
        ///     Opens the specified file using the appropriate preview handler and displays the result in this PreviewHandlerHost.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool Open(string path)
        {
            UnloadPreviewHandler();

            if (string.IsNullOrEmpty(path))
                return false;

            // try to get GUID for the preview handler
            var guid = GetPreviewHandlerGUID(path);

            if (guid == Guid.Empty)
                return false;

            CurrentPreviewHandler = guid;
            var o = Activator.CreateInstance(Type.GetTypeFromCLSID(CurrentPreviewHandler, true));

            var fileInit = o as IInitializeWithFile;

            if (fileInit == null)
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
        ///     Unloads the preview handler hosted in this PreviewHandlerHost and closes the file stream.
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
}