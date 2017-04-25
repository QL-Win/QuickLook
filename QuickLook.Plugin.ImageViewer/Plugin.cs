using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ImageViewer
{
    public class Plugin : IViewer
    {
        private ImagePanel _ip;
        private BitmapDecoder decoder;

        public int Priority { get; }

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
            decoder = BitmapDecoder.Create(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.None);
            var frame = decoder.Frames[0];

            container.SetPreferedSizeFit(new Size {Width = frame.Width, Height = frame.Height}, 0.6);
        }

        public void View(string path, ViewContentContainer container)
        {
            _ip = new ImagePanel(path);

            container.SetContent(_ip);
            container.Title = $"{Path.GetFileName(path)}";
        }

        public void Close()
        {
        }
    }
}