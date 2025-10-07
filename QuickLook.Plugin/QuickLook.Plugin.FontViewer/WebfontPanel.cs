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
using QuickLook.Common.Helpers;
using QuickLook.Plugin.HtmlViewer;
using QuickLook.Typography.OpenFont;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace QuickLook.Plugin.FontViewer;

public class WebfontPanel : WebpagePanel
{
    protected const string _resourcePrefix = "QuickLook.Plugin.FontViewer.Resources.";
    protected internal static readonly Dictionary<string, byte[]> _resources = [];
    protected byte[] _homePage;
    protected ObservableFileStream _fontStream = null;

    static WebfontPanel()
    {
        InitializeResources();
    }

    public WebfontPanel()
    {
        if (OSThemeHelper.AppsUseDarkTheme())
        {
            _webView.CreationProperties.AdditionalBrowserArguments = "--enable-features=WebContentsForceDark";
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

    public void PreviewFont(string path)
    {
        FallbackPath = Path.GetDirectoryName(path);

        var html = GenerateFontHtml(path);
        byte[] bytes = Encoding.UTF8.GetBytes(html);
        _homePage = bytes;

        NavigateToUri(new Uri("file://quicklook/"));
    }

    protected string GenerateFontHtml(string path)
    {
        string fontFamilyName = FreeTypeApi.GetFontFamilyName(path);
        var html = ReadString("/font2html.html");

        // src: url('xxx.eot');
        // src: url('xxx?#iefix') format('embedded-opentype'),
        //      url('xxx.woff') format('woff'),
        //      url('xxx.ttf') format('truetype'),
        //      url('xxx.svg#xxx') format('svg');
        var fileName = Path.GetFileName(path);
        var fileExt = Path.GetExtension(fileName);

        string cssUrl = $"src: url('{fileName}')"
            + fileExt switch
            {
                ".eot" => " format('embedded-opentype');",
                ".woff" => " format('woff');",
                ".woff2" => " format('woff2');",
                ".ttf" => " format('truetype');",
                ".otf" => " format('opentype');",
                _ => ";",
            };

        if (string.IsNullOrEmpty(fontFamilyName))
        {
            if (fileExt.ToLower().Equals(".woff2"))
            {
                fontFamilyName = Woff2.GetFontInfo(path)?.Name;
            }
        }

        // https://en.wikipedia.org/wiki/Pangram
        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        string pangram = TranslationHelper.Get("SAMPLE_TEXT", translationFile);

        html = html.Replace("--font-family;", $"font-family: '{fontFamilyName}';")
                   .Replace("--font-url;", cssUrl)
                   .Replace("{{h1}}", fontFamilyName ?? fileName)
                   .Replace("{{pangram}}", pangram ?? "The quick brown fox jumps over the lazy dog. 0123456789");

        return html;
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
                    var localPath = _fallbackPath + Uri.UnescapeDataString(requestedUri.AbsolutePath).Replace('/', '\\');

                    if (File.Exists(localPath))
                    {
                        var fileStream = new ObservableFileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                        var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                            fileStream, 200, "OK", MimeTypes.GetContentType());
                        args.Response = response;
                        _fontStream = fileStream; // Only the font request will set this
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

    public bool WaitForFontSent()
    {
        bool timeout = SpinWait.SpinUntil(
            () => _fontStream is not null && _fontStream.IsEndOfStream,
            TimeSpan.FromSeconds(1.5d) // The prediction is MAX 100MB
        );

        // Only when the `IsEndOfStream` is true
        // Delay 15ms per MB for webview2 to render the font
        if (timeout) Thread.Sleep(15 * (int)(_fontStream.Position / 1_048_576));
        return timeout;
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
}
