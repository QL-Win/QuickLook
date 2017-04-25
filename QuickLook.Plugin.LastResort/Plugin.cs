using System.Windows;

namespace QuickLook.Plugin.LastResort
{
    public class Plugin : IViewer
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

        public void Close()
        {
            _ip.Stop = true;
        }
    }
}