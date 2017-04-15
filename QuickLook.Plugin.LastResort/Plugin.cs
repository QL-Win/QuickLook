using System.Drawing;
using System.Windows;
using Size = System.Windows.Size;

namespace QuickLook.Plugin.LastResort
{
    public class Plugin : IViewer
    {
        private InfoPanel ip;
        public int Priority => -9999;

        public bool CanHandle(string sample)
        {
            return true;
        }

        public void View(string path, ViewContentContainer container)
        {
            var s = IconHelper.GetBitmapFromPath(path, IconHelper.IconSizeEnum.ExtraLargeIcon).ToBitmapSource();

            ip = new InfoPanel();
            ip.image.Source = s;

            container.SetContent(ip);
            container.PreferedSize = new Size {Width = ip.Width, Height = ip.Height};
        }

        public void Close()
        {
            //ip.Dispose();
        }
    }
}