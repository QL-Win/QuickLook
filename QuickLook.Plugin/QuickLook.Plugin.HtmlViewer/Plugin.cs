using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace QuickLook.Plugin.HtmlViewer
{
    public class Plugin : IViewer
    {
        private WebpagePanel _panel;

        public int Priority => int.MaxValue;
        public bool AllowsTransparency => false;

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            switch (Path.GetExtension(path).ToLower())
            {
                case ".html":
                case ".htm":
                    return true;

                default:
                    return false;
            }
        }

        public void Prepare(string path, ContextObject context)
        {
            context.PreferredSize = new Size(800, 800);

            context.CanFocus = true;
        }

        public void View(string path, ContextObject context)
        {
            _panel = new WebpagePanel();
            context.ViewerContent = _panel;
            context.Title = Path.IsPathRooted(path) ? Path.GetFileName(path) : path;

            _panel.Navigate(path);
            _panel.Dispatcher.Invoke(() => { context.IsBusy = false; }, DispatcherPriority.Loaded);
        }

        public void Cleanup()
        {
            GC.SuppressFinalize(this);

            _panel?.Dispose();
            _panel = null;
        }

        ~Plugin()
        {
            Cleanup();
        }
    }
}