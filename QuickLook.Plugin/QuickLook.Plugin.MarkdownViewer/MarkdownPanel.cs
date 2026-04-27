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
using QuickLook.Common.Helpers;
using QuickLook.Plugin.HtmlViewer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        content = PrepareMarkdownContent(path, content);

        var template = ReadString("/md2html.html");

        // Support automatic RTL for markdown files
        bool isRtl = false;
        if (CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
        {
            string isSupportRTL = TranslationHelper.Get("IsSupportRTL",
                failsafe: bool.TrueString,
                domain: Assembly.GetExecutingAssembly().GetName().Name);

            if (bool.TrueString.Equals(isSupportRTL, StringComparison.OrdinalIgnoreCase))
                isRtl = true;
        }

        var html = template.Replace("{{content}}", content)
                           .Replace("{{rtl}}", isRtl ? "rtl" : "ltr");

        return html;
    }

    private static string PrepareMarkdownContent(string path, string content)
    {
        var extension = Path.GetExtension(path);

        if (extension.Equals(".mermaid", StringComparison.OrdinalIgnoreCase))
            return WrapAsMermaidCodeFence(content);

        if (extension.Equals(".mmd", StringComparison.OrdinalIgnoreCase)
            && IsLikelyMermaidDocument(content))
            return WrapAsMermaidCodeFence(content);

        return content;
    }

    private static string WrapAsMermaidCodeFence(string content)
    {
        var normalized = content.Replace("\r\n", "\n").Trim('\n');
        return $"```mermaid\n{normalized}\n```";
    }

    private static bool IsLikelyMermaidDocument(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        if (content.IndexOf("```mermaid", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        using var reader = new StringReader(content);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("%%", StringComparison.Ordinal))
                continue;

            return trimmed.StartsWith("graph ", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("graph", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("flowchart ", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("flowchart", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("sequenceDiagram", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("classDiagram", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("stateDiagram", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("stateDiagram-v2", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("erDiagram", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("journey", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("gantt", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("pie", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("mindmap", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("timeline", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("gitGraph", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("quadrantChart", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("requirementDiagram", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("c4Context", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("c4Container", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("c4Component", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("c4Dynamic", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("c4Deployment", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("xychart-beta", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("block-beta", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("packet-beta", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("architecture-beta", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("kanban", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("sankey-beta", StringComparison.OrdinalIgnoreCase);
        }

        return false;
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
                    // URL path is encoded, e.g. "%20" for spaces.
                    var unescapedAbsolutePath = Uri.UnescapeDataString(requestedUri.AbsolutePath);

                    // Convert URL path to Windows path format (e.g. "/C:/Users/..." -> "C:\Users\...")
                    var potentialAbsolutePath = unescapedAbsolutePath.TrimStart('/').Replace('/', '\\');

                    string localPath;
                    // Check if it is an absolute path (e.g. ![Alt](C:\Path\To\Image.png))
                    if (Path.IsPathRooted(potentialAbsolutePath) && File.Exists(potentialAbsolutePath))
                        localPath = potentialAbsolutePath;
                    else
                        // Treat as relative path (e.g. ![Alt](Image.png))
                        localPath = _fallbackPath + unescapedAbsolutePath.Replace('/', '\\');

                    if (File.Exists(localPath))
                    {
                        var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
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

    public new static class MimeTypes
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
