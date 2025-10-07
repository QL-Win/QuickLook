// Copyright © 2017-2025 QL-Win Contributors
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
    protected Uri _currentUri;
    protected string _primaryPath;
    protected string _fallbackPath;
    protected WebView2 _webView;

    public string FallbackPath
    {
        get => _fallbackPath;
        set => _fallbackPath = value;
    }

    public WebpagePanel()
    {
        if (!Helper.IsWebView2Available())
            Content = CreateDownloadButton();
        else
            InitializeComponent();
    }

    protected virtual void InitializeComponent()
    {
        _webView = new WebView2
        {
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(SettingHelper.LocalDataPath, @"WebView2_Data\"),
            },

            // Prevent white flash in dark mode
            DefaultBackgroundColor = OSThemeHelper.AppsUseDarkTheme() ? Color.FromArgb(255, 32, 32, 32) : Color.White,
        };
        _webView.NavigationStarting += Webview_NavigationStarting;
        _webView.NavigationCompleted += WebView_NavigationCompleted;
        _webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
        Content = _webView;
    }

    public void NavigateToFile(string path)
    {
        try
        {
            _primaryPath = Path.GetDirectoryName(path);
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

    protected virtual void Webview_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
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

    protected virtual void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        _webView.DefaultBackgroundColor = Color.White; // Reset to white after page load to match expected default behavior
    }

    protected virtual void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            _webView.CoreWebView2.WebResourceRequested += WebView_WebResourceRequested;
        }
    }

    protected virtual void WebView_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(_fallbackPath) || !Directory.Exists(_fallbackPath))
        {
            return;
        }

        try
        {
            var requestedUri = new Uri(args.Request.Uri);

            if (requestedUri.Scheme == "file")
            {
                // Check if the request is for a local file
                if (!File.Exists(requestedUri.LocalPath))
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
                            var fileStream = new FileStream(fallbackFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                            var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                                fileStream, 200, "OK", "Content-Type: application/octet-stream");
                            args.Response = response;
                        }
                    }
                }
                // Check if the request exceeds MAX_PATH (260) limitation
                else if (requestedUri.LocalPath.Length >= 260)
                {
                    if (File.Exists(requestedUri.LocalPath))
                    {
                        var fileStream = new FileStream(requestedUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                        var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                            fileStream, 200, "OK", MimeTypes.GetContentType(Path.GetExtension(requestedUri.LocalPath)));
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

    public static class MimeTypes
    {
        public const string Html = "text/html";
        public const string JavaScript = "application/javascript";
        public const string Css = "text/css";
        public const string Json = "application/json";
        public const string Xml = "application/xml";
        public const string Svg = "image/svg+xml";
        public const string Png = "image/png";
        public const string Jpeg = "image/jpeg";
        public const string Gif = "image/gif";
        public const string Webp = "image/webp";
        public const string Ico = "image/x-icon";
        public const string Avif = "image/avif";
        public const string Woff = "font/woff";
        public const string Woff2 = "font/woff2";
        public const string Ttf = "font/ttf";
        public const string Otf = "font/otf";
        public const string Mp3 = "audio/mpeg";
        public const string Mp4 = "video/mp4";
        public const string Webm = "video/webm";
        public const string Pdf = "application/pdf";
        public const string Binary = "application/octet-stream";
        public const string Text = "text/plain";

        public static string GetContentType(string extension = null) => $"Content-Type: {GetMimeType(extension)}";

        /// <summary>
        /// Only handle known extensions from resources
        /// </summary>
        public static string GetMimeType(string extension = null) => extension?.ToLowerInvariant() switch
        {
            // Core web files
            ".html" or ".htm" => Html,
            ".js" => JavaScript,
            ".css" => Css,
            ".json" => Json,
            ".xml" => Xml,

            // Images
            ".png" => Png,
            ".jpg" or ".jpeg" => Jpeg,
            ".gif" => Gif,
            ".webp" => Webp,
            ".svg" => Svg,
            ".ico" => Ico,
            ".avif" => Avif,

            // Fonts
            ".woff" => Woff,
            ".woff2" => Woff2,
            ".ttf" => Ttf,
            ".otf" => Otf,

            // Media
            ".mp3" => Mp3,
            ".mp4" => Mp4,
            ".webm" => Webm,

            // Documents
            ".pdf" => Pdf,
            ".txt" => Text,

            // Archives
            ".zip" => "application/zip",
            ".gz" => "application/gzip",
            ".rar" => "application/vnd.rar",
            ".7z" => "application/x-7z-compressed",

            // Default
            _ => Binary,
        };
    }
}
