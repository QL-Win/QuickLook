// Copyright © 2017-2026 QL-Win Contributors
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

using QuickLook.Plugin.HtmlViewer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace QuickLook.Plugin.ChmViewer;

public class ChmWebpagePanel : WebpagePanel
{
    private const string _resourcePrefix = "QuickLook.Plugin.ChmViewer.Resources.";
    private static readonly Dictionary<string, byte[]> _resources = [];
    private readonly byte[] _homePage;

    static ChmWebpagePanel()
    {
        InitializeResources();
    }

    public ChmWebpagePanel()
    {
        _homePage = ReadResource("/chm2html.html") ?? [];
    }

    private static void InitializeResources()
    {
        if (_resources.Any())
            return;

        var assembly = Assembly.GetExecutingAssembly();
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(_resourcePrefix, StringComparison.Ordinal))
                continue;

            var relativePath = resourceName.Substring(_resourcePrefix.Length).Replace('\\', '/');
            if (string.IsNullOrEmpty(relativePath))
                continue;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                continue;

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            _resources[$"/{relativePath}"] = memoryStream.ToArray();
        }
    }

    public void PreviewCompiledHtmlHelp(string path)
    {
        FallbackPath = Path.GetDirectoryName(path);

        var chmFileUrl = Helper.FilePathToFileUrl(path);
        var pluginUri = new Uri($"file://quicklook/?plugin=1&chm={Uri.EscapeDataString(chmFileUrl.AbsoluteUri)}");

        NavigateToUri(pluginUri);
    }

    protected override void WebView_WebResourceRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs args)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(args.Request.Uri))
            {
                base.WebView_WebResourceRequested(sender, args);
                return;
            }

            var requestedUri = new Uri(args.Request.Uri);
            if (requestedUri.Scheme != "file")
            {
                base.WebView_WebResourceRequested(sender, args);
                return;
            }

            var requestedPath = Uri.UnescapeDataString(requestedUri.AbsolutePath);
            if (requestedPath == "/")
            {
                var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                    new MemoryStream(_homePage), 200, "OK", MimeTypes.GetContentType(".html"));
                args.Response = response;
                return;
            }

            if (ContainsKey(requestedPath))
            {
                var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                    ReadStream(requestedPath), 200, "OK", MimeTypes.GetContentType(Path.GetExtension(requestedPath)));
                args.Response = response;
                return;
            }

            base.WebView_WebResourceRequested(sender, args);
        }
        catch
        {
            base.WebView_WebResourceRequested(sender, args);
        }
    }

    private static bool ContainsKey(string key) => _resources.ContainsKey(key);

    private static Stream ReadStream(string key) => new MemoryStream(_resources[key]);

    private static byte[] ReadResource(string key) => _resources.TryGetValue(key, out var value) ? value : null;
}
