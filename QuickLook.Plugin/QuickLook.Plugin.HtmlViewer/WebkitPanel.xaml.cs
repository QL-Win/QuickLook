using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using CefSharp;

namespace QuickLook.Plugin.HtmlViewer
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class WebkitPanel : UserControl, IDisposable
    {
        private readonly string _cefPath =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

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
                    CefCommandLineArgs = {new KeyValuePair<string, string>("disable-gpu", "1")}
                });

            InitializeComponent();

            Application.Current.Exit += (sender, e) => Cef.Shutdown();

            browser.RequestHandler = new RequestHandler();
            browser.MenuHandler = new MenuHandler();
            browser.JsDialogHandler = new JsDialogHandler();
        }

        public void Dispose()
        {
            browser?.Dispose();
        }

        public void Navigate(string path)
        {
            if (Path.IsPathRooted(path))
                path = UrlHelper.FilePathToFileUrl(path);

            browser.IsBrowserInitializedChanged += (sender, e) => browser.Load(path);
        }

        public void LoadHtml(string html, string path)
        {
            if (Path.IsPathRooted(path))
                path = UrlHelper.FilePathToFileUrl(path);

            browser.IsBrowserInitializedChanged += (sender, e) => browser.LoadHtml(html, path);
        }
    }
}