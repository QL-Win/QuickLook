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

using System.IO;
using System.Windows;
using ICSharpCode.AvalonEdit.Highlighting;
using QuickLook.Common.Plugin;

namespace QuickLook.Plugin.TextViewer
{
    public class Plugin : IViewer
    {
        private TextViewerPanel _tvp;

        public int Priority => 0;

        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            const long maxSize = 20 * 1024 * 1024;

            if (path.ToLower().EndsWith(".txt"))
                return new FileInfo(path).Length <= maxSize;

            // if there is a matched highlighting scheme (by file extension), treat it as a plain text file
            if (HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(path)) != null)
                return new FileInfo(path).Length <= maxSize;

            // otherwise, read the first 512KB, check if we can get something. 
            using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                const int bufferLength = 512 * 1024;
                var buffer = new byte[bufferLength];
                var size = s.Read(buffer, 0, bufferLength);

                return IsText(buffer, size) && new FileInfo(path).Length <= maxSize;
            }
        }

        public void Prepare(string path, ContextObject context)
        {
            context.PreferredSize = new Size {Width = 800, Height = 600};
        }

        public void View(string path, ContextObject context)
        {
            _tvp = new TextViewerPanel(path, context);

            context.ViewerContent = _tvp;
            context.Title = $"{Path.GetFileName(path)}";

            context.IsBusy = false;
        }

        public void Cleanup()
        {
            _tvp.viewer = null;
        }

        private bool IsText(byte[] buffer, int size)
        {
            for (var i = 1; i < size; i++)
                if (buffer[i - 1] == 0 && buffer[i] == 0)
                    return false;

            return true;
        }
    }
}