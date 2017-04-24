using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace QuickLook.Plugin.LastResort
{
    public class Plugin : IViewer
    {
        private InfoPanel ip;
        private bool stop;
        public int Priority => -9999;

        public bool CanHandle(string sample)
        {
            return true;
        }

        public void View(string path, ViewContentContainer container)
        {
            ip = new InfoPanel(path);

            DisplayInfo(path);

            container.SetContent(ip);
            container.CanResize = false;
            container.PreferedSize = new Size {Width = ip.Width, Height = ip.Height};
        }

        public void Close()
        {
            stop = true;
        }


        private void DisplayInfo(string path)
        {
            var icon = IconHelper.GetBitmapFromPath(path, IconHelper.IconSizeEnum.ExtraLargeIcon).ToBitmapSource();

            ip.image.Source = icon;

            var name = Path.GetFileName(path);
            ip.filename.Content = string.IsNullOrEmpty(name) ? path : name;

            var last = File.GetLastWriteTime(path);
            ip.modDate.Content = $"{last.ToLongDateString()} {last.ToLongTimeString()}";

            stop = false;

            Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    var size = new FileInfo(path).Length;

                    ip.Dispatcher.Invoke(() => { ip.totalSize.Content = size.ToPrettySize(2); });
                }
                else if (Directory.Exists(path))
                {
                    long totalDirs;
                    long totalFiles;
                    long totalSize;

                    FileHelper.CountFolder(path, ref stop, out totalDirs, out totalFiles, out totalSize);

                    if (!stop)
                        ip.Dispatcher.Invoke(() =>
                        {
                            ip.totalSize.Content =
                                $"{totalSize.ToPrettySize(2)} ({totalDirs} folders and {totalFiles} files.)";
                        });
                }
            });
        }
    }
}