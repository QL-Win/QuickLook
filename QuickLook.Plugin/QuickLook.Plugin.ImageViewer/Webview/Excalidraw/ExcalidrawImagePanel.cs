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
using QuickLook.Plugin.ImageViewer.Webview.Svg;
using System;
using System.IO;
using System.Text;

namespace QuickLook.Plugin.ImageViewer.Webview.Excalidraw;

public class ExcalidrawImagePanel : SvgImagePanel
{
    private string _excalidrawContent;

    public override void Preview(string path)
    {
        FallbackPath = Path.GetDirectoryName(path);
        _excalidrawContent = File.ReadAllText(path, Encoding.UTF8);
        _homePage = _resources["/excalidraw2html.html"];
        NavigateToUri(new Uri("file://quicklook/"));
    }

    protected override void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        base.WebView_CoreWebView2InitializationCompleted(sender, e);
        if (e.IsSuccess)
            _webView.NavigationCompleted += ExcalidrawView_NavigationCompleted;
    }

    private void ExcalidrawView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess || _excalidrawContent == null)
            return;

        var jsonString = EscapeJsonString(_excalidrawContent);
        var script = $"(function(){{var x={jsonString};function r(){{if(typeof window.__EXCALIDRAW2HTML_RENDER==='function'){{window.__EXCALIDRAW2HTML_RENDER(x);}}else{{setTimeout(r,20);}}}}r();}})();";
        _ = _webView.CoreWebView2.ExecuteScriptAsync(script);
    }

    private static string EscapeJsonString(string s)
    {
        var sb = new StringBuilder(s.Length + 2);
        sb.Append('"');
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
        sb.Append('"');
        return sb.ToString();
    }
}
