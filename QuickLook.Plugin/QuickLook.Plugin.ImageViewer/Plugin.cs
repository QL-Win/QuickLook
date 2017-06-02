using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace QuickLook.Plugin.ImageViewer
{
    public class Plugin : IViewer
    {
        private static readonly string[] _formats =
        {
            // camera raw
            ".3fr", ".ari", ".arw", ".bay", ".crw", ".cr2", ".cap", ".data", ".dcs", ".dcr", ".dng", ".drf", ".eip",
            ".erf", ".fff", ".gpr", ".iiq", ".k25", ".kdc", ".mdc", ".mef", ".mos", ".mrw", ".nef", ".nrw", ".obm",
            ".orf", ".pef", ".ptx", ".pxn", ".r3d", ".raf", ".raw", ".rwl", ".rw2", ".rwz", ".sr2", ".srf", ".srw",
            ".tif", ".x3f",
            // normal
            ".bmp", ".gif", ".ico", ".jpg", ".jpeg", ".png", ".psd", ".svg", ".wdp", ".tiff", ".tga"
        };
        private Size _imageSize;
        private ImagePanel _ip;

        public int Priority => int.MaxValue;
        public bool AllowsTransparency => true;

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            return _formats.Contains(Path.GetExtension(path).ToLower());
        }

        public void Prepare(string path, ContextObject context)
        {
            // ImageMagick want to have dcraw.exe
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            _imageSize = ImageFileHelper.GetImageSize(path) ?? Size.Empty;

            if (!_imageSize.IsEmpty)
                context.SetPreferredSizeFit(_imageSize, 0.8);
            else
                context.PreferredSize = new Size(1024, 768);
        }

        public void View(string path, ContextObject context)
        {
            _ip = new ImagePanel(path);
            context.ViewerContent = _ip;
            context.Title = _imageSize.IsEmpty
                ? $"{Path.GetFileName(path)}"
                : $"{Path.GetFileName(path)} ({_imageSize.Width}×{_imageSize.Height})";

            context.IsBusy = false;
        }

        public void Cleanup()
        {
            Directory.SetCurrentDirectory(App.AppPath);
            _ip = null;
        }
    }
}