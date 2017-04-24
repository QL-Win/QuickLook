using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace QuickLook.Plugin.LastResort
{
    public class Plugin : IViewer
    {
        private InfoPanel _ip;
        private bool _stop;

        public int Priority => int.MinValue;

        public bool CanHandle(string sample)
        {
            return true;
        }

        public void BoundSize(string path, ViewContentContainer container)
        {
            _ip = new InfoPanel();

            container.CanResize = false;
            container.PreferedSize = new Size {Width = _ip.Width, Height = _ip.Height};
        }

        public void View(string path, ViewContentContainer container)
        {
            DisplayInfo(path);

            container.SetContent(_ip);
        }

        public void Close()
        {
            _stop = true;
        }


        private void DisplayInfo(string path)
        {
            var icon = IconHelper.GetBitmapFromPath(path, IconHelper.IconSizeEnum.ExtraLargeIcon).ToBitmapSource();

            _ip.image.Source = icon;

            var name = Path.GetFileName(path);
            _ip.filename.Content = string.IsNullOrEmpty(name) ? path : name;

            var last = File.GetLastWriteTime(path);
            _ip.modDate.Content = $"{last.ToLongDateString()} {last.ToLongTimeString()}";

            _stop = false;

            Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    var size = new FileInfo(path).Length;

                    _ip.Dispatcher.Invoke(() => { _ip.totalSize.Content = size.ToPrettySize(2); });
                }
                else if (Directory.Exists(path))
                {
                    long totalDirs;
                    long totalFiles;
                    long totalSize;

                    FileHelper.CountFolder(path, ref _stop, out totalDirs, out totalFiles, out totalSize);

                    if (!_stop)
                        _ip.Dispatcher.Invoke(() =>
                        {
                            _ip.totalSize.Content =
                                $"{totalSize.ToPrettySize(2)} ({totalDirs} folders and {totalFiles} files.)";
                        });
                }
            });
        }
    }
}