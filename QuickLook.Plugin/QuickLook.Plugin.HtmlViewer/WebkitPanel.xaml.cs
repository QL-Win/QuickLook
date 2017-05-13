using System;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace QuickLook.Plugin.HtmlViewer
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class WebkitPanel : UserControl
    {
        public WebkitPanel()
        {
            InitializeComponent();
        }

        public void Navigate(string path)
        {
            //path = "http://pooi.moe/QuickLook";
            if (Path.IsPathRooted(path))
                path = FilePathToFileUrl(path);

            browser.Loaded += (sender, e) => browser.Navigate(path);
        }

        private static string FilePathToFileUrl(string filePath)
        {
            StringBuilder uri = new StringBuilder();
            foreach (char v in filePath)
            {
                if ((v >= 'a' && v <= 'z') || (v >= 'A' && v <= 'Z') || (v >= '0' && v <= '9') ||
                    v == '+' || v == '/' || v == ':' || v == '.' || v == '-' || v == '_' || v == '~' ||
                    v > '\xFF')
                {
                    uri.Append(v);
                }
                else if (v == Path.DirectorySeparatorChar || v == Path.AltDirectorySeparatorChar)
                {
                    uri.Append('/');
                }
                else
                {
                    uri.Append($"%{(int) v:X2}");
                }
            }
            if (uri.Length >= 2 && uri[0] == '/' && uri[1] == '/') // UNC path
                uri.Insert(0, "file:");
            else
                uri.Insert(0, "file:///");
            return uri.ToString();
        }
    }
}