using System.Windows;

namespace QuickLook.Plugin.InfoPanel
{
    public class PluginInterface : IViewer
    {
        private InfoPanel _ip;

        public int Priority => int.MinValue;
        public bool AllowsTransparency => true;

        public void Init()
        {
        }

        public bool CanHandle(string sample)
        {
            return true;
        }

        public void Prepare(string path, ContextObject context)
        {
            context.PreferredSize = new Size {Width = 453, Height = 172};

            context.Title = "";
            context.CanResize = false;
        }

        public void View(string path, ContextObject context)
        {
            _ip = new InfoPanel();
            context.ViewerContent = _ip;

            _ip.DisplayInfo(path);

            context.IsBusy = false;
        }

        public void Cleanup()
        {
            if (_ip == null)
                return;

            _ip.Stop = true;
            _ip = null;
        }
    }
}