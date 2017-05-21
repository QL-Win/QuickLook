using System;
using System.IO;
using System.Windows;

namespace QuickLook.Plugin.ArchiveViewer
{
    public class Plugin : IViewer
    {
        private ArchiveInfoPanel _panel;

        public int Priority => 0;
        public bool AllowsTransparency => true;

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            switch (Path.GetExtension(path).ToLower())
            {
                case ".zip":
                case ".rar":
                case ".7z":
                case ".gz":
                case ".tar":
                    return true;

                default:
                    return false;
            }
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