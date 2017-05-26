using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QuickLook.Plugin.InfoPanel
{
    /// <summary>
    ///     Interaction logic for InfoPanel.xaml
    /// </summary>
    public partial class InfoPanel : UserControl
    {
        private bool _stop;

        public InfoPanel()
        {
            InitializeComponent();
        }

        public bool Stop
        {
            set => _stop = value;
            get => _stop;
        }

        public void DisplayInfo(string path)
        {
            Task.Run(() =>
            {
                var icon =
                    WindowsThumbnailProvider.GetThumbnail(path,
                        (int) (128 * DpiHelper.GetCurrentDpi().HorizontalDpi / DpiHelper.DEFAULT_DPI),
                        (int) (128 * DpiHelper.GetCurrentDpi().VerticalDpi / DpiHelper.DEFAULT_DPI),
                        ThumbnailOptions.ScaleUp);

                var source = icon.ToBitmapSource();
                icon.Dispose();

                Dispatcher.BeginInvoke(new Action(() => image.Source = source));
            });

            var name = Path.GetFileName(path);
            filename.Text = string.IsNullOrEmpty(name) ? path : name;

            var last = File.GetLastWriteTime(path);
            modDate.Text = $"Last modified at {last.ToString(CultureInfo.CurrentCulture)}";

            Stop = false;

            Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    var size = new FileInfo(path).Length;

                    Dispatcher.Invoke(() => { totalSize.Text = size.ToPrettySize(2); });
                }
                else if (Path.GetPathRoot(path) == path) // is this a drive?
                {
                    long totalSpace;
                    long totalFreeSpace;

                    FileHelper.GetDriveSpace(path, out totalSpace, out totalFreeSpace);

                    Dispatcher.Invoke(() =>
                    {
                        totalSize.Text =
                            $"Capacity {totalSpace.ToPrettySize(2)}, {totalFreeSpace.ToPrettySize(2)} available";
                    });
                }
                else if (Directory.Exists(path))
                {
                    FileHelper.CountFolder(path, ref _stop,
                        out long totalDirsL, out long totalFilesL, out long totalSizeL);

                    if (!Stop)
                        Dispatcher.Invoke(() =>
                        {
                            string t;
                            var d = totalDirsL != 0 ? $"{totalDirsL} folders" : string.Empty;
                            var f = totalFilesL != 0 ? $"{totalFilesL} files" : string.Empty;
                            if (!string.IsNullOrEmpty(d) && !string.IsNullOrEmpty(f))
                                t = $"({d} and {f})";
                            else if (string.IsNullOrEmpty(d) && string.IsNullOrEmpty(f))
                                t = string.Empty;
                            else
                                t = $"({d}{f})";

                            totalSize.Text =
                                $"{totalSizeL.ToPrettySize(2)} {t}";
                        });
                }
            });
        }
    }
}