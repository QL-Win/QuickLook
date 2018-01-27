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
using System.Windows;
using QuickLook.Common;

namespace QuickLook.Plugin.IPreviewHandlers
{
    public class Plugin : IViewer
    {
        private static readonly string[] Extensions =
        {
            ".doc", ".docx", ".docm",
            ".xls", ".xlsx", ".xlsm", ".xlsb",
            /*".vsd", ".vsdx",*/
            ".ppt", ".pptx",
            ".odt", ".ods", ".odp"
        };

        private PreviewPanel _panel;

        public int Priority => int.MaxValue;

        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            if (Extensions.Any(path.ToLower().EndsWith))
                return PreviewHandlerHost.GetPreviewHandlerGUID(path) != Guid.Empty;

            return false;
        }

        public void Prepare(string path, ContextObject context)
        {
            context.SetPreferredSizeFit(new Size {Width = 800, Height = 800}, 0.8);
        }

        public void View(string path, ContextObject context)
        {
            _panel = new PreviewPanel();
            context.ViewerContent = _panel;
            context.Title = Path.GetFileName(path);

            _panel.PreviewFile(path, context);

            context.IsBusy = false;
        }

        public void Cleanup()
        {
            GC.SuppressFinalize(this);

            _panel?.Dispose();
            _panel = null;
        }
    }
}