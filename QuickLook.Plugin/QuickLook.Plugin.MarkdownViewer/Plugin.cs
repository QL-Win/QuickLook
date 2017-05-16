using System;
using System.IO;
using System.Windows;
using QuickLook.Plugin.HtmlViewer;

namespace QuickLook.Plugin.MarkdownViewer
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
                case ".md":
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
            context.Title = Path.GetFileName(path);

            _panel.LoadHtml(GenerateMarkdownHtml(path), path);

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

        private string GenerateMarkdownHtml(string path)
        {
            var md = File.ReadAllText(path);
            var html = Resources.md2html.Replace("{{content}}", md);

            return html;
        }
    }
}