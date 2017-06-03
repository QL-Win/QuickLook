using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QuickLook.Plugin.HtmlViewer
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class WebpagePanel : UserControl, IDisposable
    {
        public WebpagePanel()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            browser?.Dispose();
            browser = null;
        }

        public void Navigate(string path)
        {
            if (Path.IsPathRooted(path))
                path = Helper.FilePathToFileUrl(path);

            browser.Dispatcher.Invoke(() => { browser.Navigate(path); }, DispatcherPriority.Loaded);
        }

        public void LoadHtml(string html)
        {
            var s = new MemoryStream(Encoding.UTF8.GetBytes(html ?? ""));

            browser.Dispatcher.Invoke(() => { browser.Navigate(s); }, DispatcherPriority.Loaded);
        }
    }
}