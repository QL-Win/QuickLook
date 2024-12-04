// Copyright © 2021 Paddy Xu and Frank Becker
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using QuickLook.Common.Helpers;

namespace QuickLook.Plugin.HtmlViewer
{
    public class WebpagePanel : UserControl
    {
        public Uri _currentUri;
        public WebView2 _webView;

        public WebpagePanel()
        {
            if (!Helper.IsWebView2Available())
            {
                Content = CreateDownloadButton();
            }
            else
            {
                _webView = new WebView2
                {
                    CreationProperties = new CoreWebView2CreationProperties
                    {
                        UserDataFolder = Path.Combine(SettingHelper.LocalDataPath, @"WebView2_Data\\"),
                    },
                    DefaultBackgroundColor = OSThemeHelper.AppsUseDarkTheme() ? Color.FromArgb(255, 32, 32, 32) : Color.White, // Prevent white flash in dark mode
                };
                _webView.NavigationStarting += NavigationStarting_CancelNavigation;
                _webView.NavigationCompleted += WebView_NavigationCompleted;
                Content = _webView;
            }
        }

        public void NavigateToFile(string path)
        {
            var uri = Path.IsPathRooted(path) ? Helper.FilePathToFileUrl(path) : new Uri(path);

            NavigateToUri(uri);
        }

        public void NavigateToUri(Uri uri)
        {
            if (_webView == null)
                return;

            _webView.Source = uri;
            _currentUri = _webView.Source;
        }

        public void NavigateToHtml(string html)
        {
            _webView?.EnsureCoreWebView2Async()
                .ContinueWith(_ => Dispatcher.Invoke(() => _webView?.NavigateToString(html)));
        }

        private void NavigationStarting_CancelNavigation(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.StartsWith("data:")) // when using NavigateToString
                return;

            var newUri = new Uri(e.Uri);
            if (newUri != _currentUri) e.Cancel = true;
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _webView.DefaultBackgroundColor = Color.White; // Reset to white after page load to match expected default behavior
        }

        public void Dispose()
        {
            _webView?.Dispose();
            _webView = null;
        }

        private object CreateDownloadButton()
        {
            var button = new Button
            {
                Content = TranslationHelper.Get("WEBVIEW2_NOT_AVAILABLE",
                    domain: Assembly.GetExecutingAssembly().GetName().Name),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(20, 6, 20, 6)
            };
            button.Click += (sender, e) => Process.Start("https://go.microsoft.com/fwlink/p/?LinkId=2124703");

            return button;
        }
    }
}
