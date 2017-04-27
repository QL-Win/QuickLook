using System.IO;
using System.Windows;

namespace QuickLook.Plugin.ImageViewer
{
    public class Plugin : IViewer
    {
        private Size _imageSize;
        private ImagePanel _ip;

        public int Priority => 9999;

        public bool CanHandle(string path)
        {
            // TODO: determine file type by content

            if (Directory.Exists(path))
                return false;

            switch (Path.GetExtension(path).ToLower())
            {
                case ".bmp":
                case ".gif":
                case ".ico":
                case ".jpg":
                case ".png":
                case ".wdp":
                case ".tiff":
                    return true;

                default:
                    return false;
            }
        }

        public void Prepare(string path, ViewContentContainer container)
        {
            _imageSize = ImageFileHelper.GetImageSize(path);

            container.SetPreferedSizeFit(_imageSize, 0.8);
        }

        public void View(string path, ViewContentContainer container)
        {
            _ip = new ImagePanel(path);

            container.SetContent(_ip);
            container.Title = $"{Path.GetFileName(path)} ({_imageSize.Width} × {_imageSize.Height})";
        }

        public void Close()
        {
        }
    }
}