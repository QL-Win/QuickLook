// Copyright © 2018 Marco Gavelli and Paddy Xu
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
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using QuickLook.Common.Plugin;
using VersOne.Epub;

namespace QuickLook.Plugin.EpubViewer
{
    public class Plugin : IViewer
    {
        private ContextObject _context;
        private EpubViewerControl _epubControl;
        public int Priority => 0;

        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            return !Directory.Exists(path) && path.ToLower().EndsWith(".epub");
        }

        public void Cleanup()
        {
            _epubControl.Dispose();
            _epubControl = null;
        }

        public void Prepare(string path, ContextObject context)
        {
            _context = context;
            context.SetPreferredSizeFit(new Size {Width = 1000, Height = 800}, 0.8);
        }

        public void View(string path, ContextObject context)
        {
            _epubControl = new EpubViewerControl(context);
            context.ViewerContent = _epubControl;
            Exception exception = null;

            _epubControl.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Opens a book
                    var epubBook = EpubReader.OpenBook(path);
                    context.Title = epubBook.Title;
                    _epubControl.SetContent(epubBook);
                    context.IsBusy = false;
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }), DispatcherPriority.Loaded).Wait();

            if (exception != null)
                ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}