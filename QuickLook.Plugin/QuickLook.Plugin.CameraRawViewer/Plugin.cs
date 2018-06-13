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
using System.IO;
using System.Linq;
using QuickLook.Common.Plugin;

namespace QuickLook.Plugin.CameraRawViewer
{
    public class Plugin : IViewer
    {
        private static readonly string[] Formats =
        {
            // camera raw
            ".ari", ".arw", ".bay", ".crw", ".cr2", ".cap", ".dcs", ".dcr", ".dng", ".drf", ".eip", ".erf", ".fff",
            ".iiq", ".k25", ".kdc", ".mdc", ".mef", ".mos", ".mrw", ".nef", ".nrw", ".obm", ".orf", ".pef", ".ptx",
            ".pxn", ".r3d", ".raf", ".raw", ".rwl", ".rw2", ".rwz", ".sr2", ".srf", ".srw", ".x3f"
        };
        private string _image = string.Empty;

        private ImageViewer.Plugin _imageViewierPlugin;

        public int Priority => -1;//int.MaxValue;

        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            return false;
            return !Directory.Exists(path) && Formats.Any(path.ToLower().EndsWith);
        }

        public void Prepare(string path, ContextObject context)
        {
            _imageViewierPlugin=new ImageViewer.Plugin();

            _imageViewierPlugin.Prepare(path, context);
        }

        public void View(string path, ContextObject context)
        {
            _image = DCraw.ConvertToTiff(path);

            if (string.IsNullOrEmpty(_image))
                throw new Exception("DCraw failed.");

            _imageViewierPlugin.View(_image, context);

            // correct title
            context.Title = Path.GetFileName(path);
        }

        public void Cleanup()
        {
            _imageViewierPlugin.Cleanup();
            _imageViewierPlugin = null;

            try
            {
                File.Delete(_image);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}