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
using QuickLook.Plugin.HtmlViewer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickLook.Plugin.ImageViewer.Webview.Svg;

public class SvgImagePanel : WebpagePanel, IWebImagePanel
{
    protected const string _resourcePrefix = "QuickLook.Plugin.ImageViewer.Resources.";
    protected internal static readonly Dictionary<string, byte[]> _resources = [];
    protected byte[] _homePage;

    private object _objectForScripting;

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

    protected override void InitializeComponent()
    {
        _webView = new WebView2()
        {
            CreationProperties = new CoreWebView2CreationProperties
            {
                UserDataFolder = Path.Combine(SettingHelper.LocalDataPath, @"WebView2_Data\"),
            },
        };
        _webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
        Content = _webView;
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
        NavigateToUri(new Uri("file://quicklook/"));
    }

    protected override void WebView_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs args)
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
                    var localPath = _fallbackPath + requestedUri.AbsolutePath.Replace('/', '\\');

                    if (File.Exists(localPath))
                    {
                        var fileStream = File.OpenRead(localPath);
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
                        var fileStream = File.OpenRead(localPath);
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

    public static class MimeTypes
    {
        public const string Html = "text/html";
        public const string JavaScript = "application/javascript";
        public const string Json = "application/json";
        public const string Css = "text/css";
        public const string Binary = "application/octet-stream";

        public static string GetContentTypeHeader(string extension = null) => $"Content-Type: {GetMimeType(extension)}";

        public static string GetMimeType(string extension = null) => extension?.ToLowerInvariant() switch
        {
            ".js" => JavaScript, // Only handle known extensions from resources
            ".json" => Json,
            ".css" => Css,
            ".html" => Html,
            _ => Binary,
        };
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
