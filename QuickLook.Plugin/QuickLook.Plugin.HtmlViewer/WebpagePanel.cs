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

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using QuickLook.Common.Helpers;
using QuickLook.Plugin.HtmlViewer.NativeMethods;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace QuickLook.Plugin.HtmlViewer;

public class WebpagePanel : UserControl
{
    private Uri _currentUri;
    private string _primaryPath;
    private string _fallbackPath;
    private WebView2 _webView;

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
            _webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
            Content = _webView;
        }
    }

    public void NavigateToFile(string path, string fallbackPath = null)
    {
        try
        {
            _primaryPath = Path.GetDirectoryName(path);
            _fallbackPath = fallbackPath;
        }
        catch (Exception e)
        {
            // Omit logging for less important logs
            Debug.WriteLine(e);
        }

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
        if (newUri == _currentUri) return;
        e.Cancel = true;

        // Open in default browser
        try
        {
            if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
            {
                Debug.WriteLine($"Invalid URI format: {e.Uri}");
                return;
            }

            // Safe schemes can open directly
            if (uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps ||
                uri.Scheme == Uri.UriSchemeMailto)
            {
                try
                {
                    Process.Start(uri.AbsoluteUri);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            // Ask user for unsafe schemes. Use dispatcher to avoid blocking thread.
            string associatedApp = ShlwApi.GetAssociatedAppForScheme(uri.Scheme);
            _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // TODO: translation
                var result = MessageBox.Show(
                    !string.IsNullOrEmpty(associatedApp) ?
                    $"The following link will open in {associatedApp}:\n{e.Uri}" : $"The following link will open:\n{e.Uri}",
                    !string.IsNullOrEmpty(associatedApp) ?
                    $"Open {associatedApp}?" : "Open custom URI?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start(e.Uri);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }

    private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        _webView.DefaultBackgroundColor = Color.White; // Reset to white after page load to match expected default behavior
    }

    private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

            _webView.CoreWebView2.WebResourceRequested += (sender, args) =>
            {
                if (string.IsNullOrWhiteSpace(_fallbackPath) || !Directory.Exists(_fallbackPath))
                {
                    return;
                }

                try
                {
                    var requestedUri = new Uri(args.Request.Uri);

                    // Check if the request is for a local file
                    if (requestedUri.Scheme == "file" && !File.Exists(requestedUri.LocalPath))
                    {
                        // Try loading from fallback directory
                        var fileName = Path.GetFileName(requestedUri.LocalPath);
                        var fileDirectoryName = Path.GetDirectoryName(requestedUri.LocalPath);

                        // Convert the primary path to fallback path
                        if (fileDirectoryName.StartsWith(_primaryPath))
                        {
                            var fallbackFilePath = Path.Combine(
                                _fallbackPath.Trim('/', '\\'), // Make it combinable
                                fileDirectoryName.Substring(_primaryPath.Length).Trim('/', '\\'), // Make it combinable
                                fileName
                            );

                            if (File.Exists(fallbackFilePath))
                            {
                                // Serve the file from the fallback directory
                                var fileStream = File.OpenRead(fallbackFilePath);
                                var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                                    fileStream, 200, "OK", "Content-Type: application/octet-stream");
                                args.Response = response;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // We don't need to feel burdened by any exceptions
                    Debug.WriteLine(e);
                }
            };
        }
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
