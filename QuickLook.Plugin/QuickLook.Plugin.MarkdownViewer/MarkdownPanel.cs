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
using QuickLook.Plugin.HtmlViewer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UtfUnknown;

namespace QuickLook.Plugin.MarkdownViewer;

public class MarkdownPanel : WebpagePanel
{
    protected const string _resourcePrefix = "QuickLook.Plugin.MarkdownViewer.Resources.";
    protected internal static readonly Dictionary<string, byte[]> _resources = [];
    protected byte[] _homePage;

    static MarkdownPanel()
    {
        InitializeResources();
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

    public void PreviewMarkdown(string path)
    {
        FallbackPath = Path.GetDirectoryName(path);

        var html = GenerateMarkdownHtml(path);
        byte[] bytes = Encoding.UTF8.GetBytes(html);
        _homePage = bytes;

        NavigateToUri(new Uri("file://quicklook/"));
    }

    protected string GenerateMarkdownHtml(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var encoding = CharsetDetector.DetectFromBytes(bytes).Detected?.Encoding ?? Encoding.Default;
        var content = encoding.GetString(bytes);

        var template = ReadString("/md2html.html");
        var html = template.Replace("{{content}}", content);

        return html;
    }

    protected override void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            _webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

            _webView.CoreWebView2.WebResourceRequested += (sender, args) =>
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
                                new MemoryStream(_homePage), 200, "OK", MimeTypes.GetContentType(".html"));
                            args.Response = response;
                        }
                        else if (ContainsKey(requestedUri.AbsolutePath))
                        {
                            var stream = ReadStream(requestedUri.AbsolutePath);
                            var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                                stream, 200, "OK", MimeTypes.GetContentType(Path.GetExtension(requestedUri.AbsolutePath)));
                            args.Response = response;
                        }
                        else
                        {
                            var localPath = _fallbackPath + requestedUri.AbsolutePath.Replace('/', '\\');

                            if (File.Exists(localPath))
                            {
                                var fileStream = File.OpenRead(localPath);
                                var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                                    fileStream, 200, "OK", MimeTypes.GetContentType());
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
        public const string Css = "text/css";
        public const string Binary = "application/octet-stream";

        public static string GetContentType(string extension = null) => $"Content-Type: {GetMimeType(extension)}";

        public static string GetMimeType(string extension = null) => extension?.ToLowerInvariant() switch
        {
            ".js" => JavaScript, // Only handle known extensions from resources
            ".css" => Css,
            ".html" => Html,
            _ => Binary,
        };
    }
}
