using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using CefSharp;

namespace QuickLook.Plugin.HtmlViewer
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class WebkitPanel : UserControl,IDisposable
    {
        private string _cefPath =
            Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));

        public WebkitPanel()
        {
            var libraryLoader = new CefLibraryHandle(Path.Combine(_cefPath, "libcef.dll"));

            if (!Cef.IsInitialized)
                Cef.Initialize(new CefSettings
                {
                    BrowserSubprocessPath = Path.Combine(_cefPath, "CefSharp.BrowserSubprocess.exe"),
                    LocalesDirPath = Path.Combine(_cefPath, "locales"),
                    ResourcesDirPath = _cefPath,
                    LogSeverity = LogSeverity.Disable,
                    CefCommandLineArgs = {new KeyValuePair<string, string>("disable-gpu", "1")},
                });

            InitializeComponent();

            Application.Current.Exit += (sender, e) => Cef.Shutdown();
        }

        public void Navigate(string path)
        {
            if (Path.IsPathRooted(path))
                path = FilePathToFileUrl(path);

            browser.IsBrowserInitializedChanged += (sender, e) => browser.Load(path);
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
        
        public void Dispose()
        {
            browser?.Dispose();
        }
    }
}