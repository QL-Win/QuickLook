using System;
using System.IO;
using System.Windows;

namespace QuickLook.Plugin.HtmlViewer
{
    public class Plugin : IViewer
    {
        private WebkitPanel _panel;

        public int Priority => int.MaxValue;

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

            context.Focusable = true;
        }

        public void View(string path, ContextObject context)
        {
            _panel = new WebkitPanel();
            context.ViewerContent = _panel;
            context.Title = Path.IsPathRooted(path) ? Path.GetFileName(path) : path;

            _panel.Navigate(path);
            context.IsBusy = false;
        }

        public void Cleanup()
        {
            GC.SuppressFinalize(this);

            _panel?.Dispose();
        }

        ~Plugin()
        {
            Cleanup();
        }
    }
}