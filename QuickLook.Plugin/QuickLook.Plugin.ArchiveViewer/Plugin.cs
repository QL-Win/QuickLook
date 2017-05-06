using System;
using System.IO;
using System.Windows;
using SharpCompress.Archives;

namespace QuickLook.Plugin.ArchiveViewer
{
    public class Plugin : IViewer
    {
        private ArchiveInfoPanel _panel;

        public int Priority => 0;

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            using (var stream = File.OpenRead(path))
            {
                try
                {
                    ArchiveFactory.Open(stream);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }

        public void Prepare(string path, ContextObject context)
        {
            context.PreferredSize = new Size {Width = 800, Height = 600};
        }

        public void View(string path, ContextObject context)
        {
            _panel = new ArchiveInfoPanel(path);

            context.ViewerContent = _panel;
            context.Title = $"{Path.GetFileName(path)}";

            context.IsBusy = false;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _panel?.Dispose();
        }

        ~Plugin()
        {
            Dispose();
        }
    }
}