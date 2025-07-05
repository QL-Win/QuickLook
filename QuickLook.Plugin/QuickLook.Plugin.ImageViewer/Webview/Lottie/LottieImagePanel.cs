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
using QuickLook.Plugin.ImageViewer.Webview.Svg;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickLook.Plugin.ImageViewer.Webview.Lottie;

public class LottieImagePanel : SvgImagePanel
{
    public override void Preview(string path)
    {
        FallbackPath = Path.GetDirectoryName(path);

        ObjectForScripting ??= new ScriptHandler(path);

        _homePage = _resources["/lottie2html.html"];
        NavigateToUri(new Uri("file://quicklook/"));
    }

    protected override void WebView_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        try
        {
            var requestedUri = new Uri(args.Request.Uri);

            if ((requestedUri.Scheme == "https" || requestedUri.Scheme == "http")
              && requestedUri.AbsolutePath.EndsWith(".lottie", StringComparison.OrdinalIgnoreCase))
            {
                var localPath = Uri.UnescapeDataString($"{requestedUri.Authority}:{requestedUri.AbsolutePath}".Replace('/', '\\'));

                if (localPath.StartsWith(_fallbackPath, StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(localPath))
                    {
                        var content = LottieExtractor.GetJsonContent(localPath);
                        byte[] byteArray = Encoding.UTF8.GetBytes(content);
                        var stream = new MemoryStream(byteArray);
                        var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                            stream, 200, "OK",
                            $"""
                            Access-Control-Allow-Origin: *
                            Content-Type: {MimeTypes.GetMimeType()}
                            """
                        );
                        args.Response = response;
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            // We don't need to feel burdened by any exceptions
            Debug.WriteLine(e);
        }

        base.WebView_WebResourceRequested(sender, args);
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
}
