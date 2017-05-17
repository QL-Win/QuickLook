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

        public void Prepare(string path, ContextObject context)
        {
            _imageSize = ImageFileHelper.GetImageSize(path);

            context.SetPreferredSizeFit(_imageSize, 0.8);
        }

        public void View(string path, ContextObject context)
        {
            _ip = new ImagePanel(path);

            context.ViewerContent = _ip;
            context.Title = $"{Path.GetFileName(path)} ({_imageSize.Width}×{_imageSize.Height})";

            context.IsBusy = false;
        }

        public void Cleanup()
        {
            _ip = null;
        }
    }
}