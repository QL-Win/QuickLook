using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.HtmlViewer;
using VersOne.Epub;

namespace QuickLook.Plugin.EpubViewer
{
    public class Plugin : IViewer
    {
        private EpubViewerControl _panel;
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
        }

        public void Prepare(string path, ContextObject context)
        {
            context.PreferredSize = new Size { Width = 1000, Height = 600 };
        }

        public void View(string path, ContextObject context)
        {
            _panel = new EpubViewerControl();
            context.ViewerContent = _panel;
            Exception exception = null;

            _panel.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // Opens a book
                    EpubBookRef epubBook = EpubReader.OpenBook(path);
                    context.Title = epubBook.Title;
                    _panel.SetContent(epubBook);

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
