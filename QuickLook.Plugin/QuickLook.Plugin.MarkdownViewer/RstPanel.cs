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

using Microsoft.Web.WebView2.Core;
using QuickLook.Plugin.HtmlViewer;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace QuickLook.Plugin.MarkdownViewer;

public class RstPanel : WebpagePanel
{
    public static readonly string[] Extensions =
    [
        ".rst", // reStructuredText (preferred)
        ".restructuredtext", // Explicit / uncommon
        // Note: .rest is intentionally omitted — reserved for HTTP REST client files in TextViewer
    ];

    private byte[] _homePage;
    private byte[] _rstBytes;
    private string _rstName;

    public static bool CanHandle(string path)
    {
        if (string.IsNullOrEmpty(path) || Directory.Exists(path))
            return false;

        var extension = Path.GetExtension(path);
        return Extensions.Any(ext => extension.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public void PreviewRst(string path)
    {
        FallbackPath = Path.GetDirectoryName(path);
        _rstBytes = File.ReadAllBytes(path);
        _rstName = Path.GetFileName(path);
        _homePage = Encoding.UTF8.GetBytes(MarkdownPanel.ReadString("/rst2html.html"));

        NavigateToUri(new Uri("file://quicklook/"));
    }

    protected override void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        base.WebView_NavigationCompleted(sender, e);

        if (!e.IsSuccess || _rstBytes == null || _webView?.CoreWebView2 == null)
            return;

        var base64 = Convert.ToBase64String(_rstBytes);
        var nameJson = EscapeJsonString(_rstName ?? "file.rst");
        var json = $"{{\"type\":\"open-rst\",\"payload\":{{\"base64\":\"{base64}\",\"name\":{nameJson}}}}}";
        _webView.CoreWebView2.PostWebMessageAsJson(json);
    }

    protected override void WebView_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        Debug.WriteLine($"[{args.Request.Method}] {args.Request.Uri}");

        try
        {
            var requestedUri = new Uri(args.Request.Uri);

            if (requestedUri.Scheme != "file")
                return;

            if (requestedUri.AbsolutePath == "/")
            {
                var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                    new MemoryStream(_homePage), 200, "OK", MarkdownPanel.MimeTypes.GetContentType(".html"));
                args.Response = response;
                return;
            }

            if (MarkdownPanel.ContainsKey(requestedUri.AbsolutePath))
            {
                var stream = MarkdownPanel.ReadStream(requestedUri.AbsolutePath);
                var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                    stream, 200, "OK", MarkdownPanel.MimeTypes.GetContentType(Path.GetExtension(requestedUri.AbsolutePath)));
                args.Response = response;
                return;
            }

            // URL path is encoded, e.g. "%20" for spaces.
            var unescapedAbsolutePath = Uri.UnescapeDataString(requestedUri.AbsolutePath);

            // Convert URL path to Windows path format (e.g. "/C:/Users/..." -> "C:\Users\...")
            var potentialAbsolutePath = unescapedAbsolutePath.TrimStart('/').Replace('/', '\\');

            string localPath;
            // Check if it is an absolute path
            if (Path.IsPathRooted(potentialAbsolutePath) && File.Exists(potentialAbsolutePath))
                localPath = potentialAbsolutePath;
            else
                // Treat as relative path (e.g. image directives)
                localPath = _fallbackPath + unescapedAbsolutePath.Replace('/', '\\');

            if (File.Exists(localPath))
            {
                var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                    fileStream, 200, "OK", MarkdownPanel.MimeTypes.GetContentType());
                args.Response = response;
            }
        }
        catch (Exception e)
        {
            // We don't need to feel burdened by any exceptions
            Debug.WriteLine(e);
        }
    }

    private static string EscapeJsonString(string s)
    {
        var sb = new StringBuilder((s?.Length ?? 0) + 2);
        sb.Append('"');
        if (s != null)
        {
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.Append($"\\u{(int)c:x4}");
                        else
                            sb.Append(c);
                        break;
                }
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}
