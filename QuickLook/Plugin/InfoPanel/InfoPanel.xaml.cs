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
            var icon =
                WindowsThumbnailProvider.GetThumbnail(path, 256, 256,
                    ThumbnailOptions.ScaleUp);

            image.Source = icon.ToBitmapSource();

            icon.Dispose();

            var name = Path.GetFileName(path);
            filename.Content = string.IsNullOrEmpty(name) ? path : name;

            var last = File.GetLastWriteTime(path);
            modDate.Content = last.ToString(CultureInfo.CurrentCulture);

            Stop = false;

            Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    var size = new FileInfo(path).Length;

                    Dispatcher.Invoke(() => { totalSize.Content = size.ToPrettySize(2); });
                }
                else if (Directory.Exists(path))
                {
                    long totalDirsL;
                    long totalFilesL;
                    long totalSizeL;

                    FileHelper.CountFolder(path, ref _stop, out totalDirsL, out totalFilesL, out totalSizeL);

                    if (!Stop)
                        Dispatcher.Invoke(() =>
                        {
                            totalSize.Content =
                                $"{totalSizeL.ToPrettySize(2)} ({totalDirsL} folders and {totalFilesL} files)";
                        });
                }
            });
        }
    }
}