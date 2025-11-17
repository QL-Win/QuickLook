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
using QuickLook.Common.Plugin;
using QuickLook.Plugin.HtmlViewer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace QuickLook.Plugin.ImageViewer.Webview.Svg;

public partial class SvgImagePanel : UserControl, IWebImagePanel
{
    protected const string _resourcePrefix = "QuickLook.Plugin.ImageViewer.Resources.";
    protected internal static readonly Dictionary<string, byte[]> _resources = [];
    protected byte[] _homePage;
    protected WebView2 _webView;
    protected Uri _currentUri;
    protected string _primaryPath;
    protected string _fallbackPath;
    private ContextObject _contextObject;

    private object _objectForScripting;

    public string FallbackPath
    {
        get => _fallbackPath;
        set => _fallbackPath = value;
    }

    public ContextObject ContextObject
    {
        get => _contextObject;
        set => _contextObject = value;
    }

    public object ObjectForScripting
    {
        get => _objectForScripting;
        set
        {
            _objectForScripting = value;
            _webView?.EnsureCoreWebView2Async()
                .ContinueWith(_ =>
                    _webView?.Dispatcher.Invoke(() =>
                        _webView?.CoreWebView2.AddHostObjectToScript("external", value)
                    )
                );
        }
    }

    static SvgImagePanel()
    {
        InitializeResources();
    }

    public SvgImagePanel()
    {
        InitializeComponent();

        // Clear merged dictionaries from design time
        Resources.MergedDictionaries.Clear();

        // Initialize WebView2
        InitializeWebView();

        // Wire up the theme toggle button
        buttonBackgroundColour.Click += OnBackgroundColourOnClick;
    }

    protected void InitializeWebView()
    {
        _webView = new WebView2()
        {
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(SettingHelper.LocalDataPath, @"WebView2_Data\"),
            },
            DefaultBackgroundColor = Color.Transparent,
        };
        _webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
        
        // Add WebView2 to the Grid at the first position (behind the button)
        var grid = (Grid)Content;
        grid.Children.Insert(0, _webView);
    }

    private void OnBackgroundColourOnClick(object sender, RoutedEventArgs e)
    {
        if (ContextObject == null) return;

        // Toggle the theme
        var newTheme = ContextObject.Theme == Themes.Dark ? Themes.Light : Themes.Dark;
        ContextObject.Theme = newTheme;

        // Save the theme preference
        SettingHelper.Set("LastTheme", (int)newTheme, "QuickLook.Plugin.ImageViewer");

        // Update WebView2 background color
        UpdateWebViewBackgroundColor();
    }

    private void UpdateWebViewBackgroundColor()
    {
        if (_webView == null) return;

        var isDark = ContextObject?.Theme == Themes.Dark;
        _webView.DefaultBackgroundColor = isDark 
            ? Color.FromArgb(255, 32, 32, 32) 
            : Color.White;
    }

    public void NavigateToUri(Uri uri)
    {
        if (_webView == null)
            return;

        _webView.Source = uri;
        _currentUri = _webView.Source;
    }

    protected virtual void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            _webView.CoreWebView2.WebResourceRequested += WebView_WebResourceRequested;
        }
    }

    protected static void InitializeResources()
    {
        if (_resources.Any()) return;

        var assembly = Assembly.GetExecutingAssembly();

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(_resourcePrefix)) continue;

            var relativePath = resourceName.Substring(_resourcePrefix.Length);
            if (relativePath.Equals("resources", StringComparison.OrdinalIgnoreCase)) continue;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) continue;
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            _resources.Add($"/{relativePath.Replace('\\', '/')}", memoryStream.ToArray());
        }
    }

    public virtual void Preview(string path)
    {
        FallbackPath = Path.GetDirectoryName(path);

        ObjectForScripting ??= new ScriptHandler(path);

        _homePage = _resources["/svg2html.html"];
        
        // Update WebView2 background color based on current theme
        UpdateWebViewBackgroundColor();
        
        NavigateToUri(new Uri("file://quicklook/"));
    }

    protected virtual void WebView_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        Debug.WriteLine($"[{args.Request.Method}] {args.Request.Uri}");

        try
        {
            var requestedUri = new Uri(args.Request.Uri);

            if (requestedUri.Scheme == "file")
            {
                if (requestedUri.AbsolutePath == "/")
                {
                    var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                        new MemoryStream(_homePage), 200, "OK", MimeTypes.GetContentTypeHeader(".html"));
                    args.Response = response;
                }
                else if (ContainsKey(requestedUri.AbsolutePath))
                {
                    var stream = ReadStream(requestedUri.AbsolutePath);
                    var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                        stream, 200, "OK", MimeTypes.GetContentTypeHeader(Path.GetExtension(requestedUri.AbsolutePath)));
                    args.Response = response;
                }
                else
                {
                    var localPath = _fallbackPath + Uri.UnescapeDataString(requestedUri.AbsolutePath).Replace('/', '\\');

                    if (File.Exists(localPath))
                    {
                        var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                        var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                            fileStream, 200, "OK", MimeTypes.GetContentTypeHeader());
                        args.Response = response;
                    }
                }
            }
            else if (requestedUri.Scheme == "https" || requestedUri.Scheme == "http")
            {
                var localPath = Uri.UnescapeDataString($"{requestedUri.Authority}:{requestedUri.AbsolutePath}".Replace('/', '\\'));

                if (localPath.StartsWith(_fallbackPath, StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(localPath))
                    {
                        var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                        var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                            fileStream, 200, "OK",
                            $"""
                            Access-Control-Allow-Origin: *
                            Content-Type: {MimeTypes.GetMimeType()}
                            """
                        );
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

    public static bool ContainsKey(string key)
    {
        return _resources.ContainsKey(key);
    }

    public static Stream ReadStream(string key)
    {
        byte[] bytes = _resources[key];
        return new MemoryStream(bytes);
    }

    public static string ReadString(string key)
    {
        using var reader = new StreamReader(ReadStream(key), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public void Dispose()
    {
        _webView?.Dispose();
        _webView = null;
    }

    public static class MimeTypes
    {
        public static string GetContentTypeHeader(string extension = null)
            => $"Content-Type: {WebpagePanel.MimeTypes.GetMimeType(extension)}";

        public static string GetMimeType(string extension = null)
            => WebpagePanel.MimeTypes.GetMimeType(extension);
    }
}

[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public sealed class ScriptHandler(string path)
{
    public string Path { get; } = path;

    public async Task<string> GetPath()
    {
        return await Task.FromResult(new Uri(Path).AbsolutePath);
    }

    public async Task<string> GetSvgContent()
    {
        if (File.Exists(Path))
        {
            var bytes = File.ReadAllBytes(Path);
            return await Task.FromResult(Encoding.UTF8.GetString(bytes));
        }
        return string.Empty;
    }
}
