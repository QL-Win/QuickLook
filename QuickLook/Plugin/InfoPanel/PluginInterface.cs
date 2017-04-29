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

        public void Prepare(string path, ViewContentContainer container)
        {
            _ip = new InfoPanel();

            container.CanResize = false;
            container.PreferedSize = new Size {Width = _ip.Width, Height = _ip.Height};
        }

        public void View(string path, ViewContentContainer container)
        {
            _ip.DisplayInfo(path);

            container.SetContent(_ip);
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