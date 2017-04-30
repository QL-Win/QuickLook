using System;
using System.Windows;

namespace QuickLook.Plugin.InfoPanel
{
    public class PluginInterface : IViewer, IDisposable
    {
        private InfoPanel _ip;

        public int Priority => int.MinValue;

        public bool CanHandle(string sample)
        {
            return true;
        }

        public void BoundViewSize(string path, ViewerObject context)
        {
            _ip = new InfoPanel();

            context.CanResize = false;
            context.PreferredSize = new Size {Width = _ip.Width, Height = _ip.Height};
        }

        public void View(string path, ViewerObject context)
        {
            _ip.DisplayInfo(path);

            context.ViewerContent = _ip;

            context.IsBusy = false;
        }

        public void Dispose()
        {
            _ip.Stop = true;
        }

        ~PluginInterface()
        {
            Dispose();
        }
    }
}