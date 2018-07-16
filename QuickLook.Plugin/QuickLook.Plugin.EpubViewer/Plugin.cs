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
        public int Priority => int.MaxValue;

        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            return !Directory.Exists(path) && new[] { ".epub" }.Any(path.ToLower().EndsWith);
        }

        public void Cleanup()
        {
            _epubControl = null;
            _context = null;
        }

        public void Prepare(string path, ContextObject context)
        {
            _context = context;
            context.SetPreferredSizeFit(new Size { Width = 1000, Height = 600 }, 0.8);
        }

        public void View(string path, ContextObject context)
        {
            _epubControl = new EpubViewerControl();
            context.ViewerContent = _epubControl;
            Exception exception = null;

            _epubControl.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Opens a book
                    EpubBookRef epubBook = EpubReader.OpenBook(path);
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
