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

#if false // The current Graphviz rendering is not suitable for force dark mode
using QuickLook.Common.Helpers;
using QuickLook.Plugin.HtmlViewer;
using System.Reflection;
#endif

namespace QuickLook.Plugin.ImageViewer.Webview.Graphviz;

public class GvImagePanel : SvgImagePanel
{
    private string _dotContent;

    public GvImagePanel()
    {
#if false // The current Graphviz rendering is not suitable for force dark mode
        if (OSThemeHelper.AppsUseDarkTheme())
        {
            // Invoke using reflection: WebView2.CreationProperties.AdditionalBrowserArguments
            // This approach allows the library to avoid direct dependency on WebView2
            if (typeof(WebpagePanel).GetField("_webView", BindingFlags.NonPublic | BindingFlags.Instance) is FieldInfo fieldInfo)
            {
                object webView2 = fieldInfo.GetValue(this);

                if (webView2?.GetType().GetProperty("CreationProperties", BindingFlags.Public | BindingFlags.Instance) is PropertyInfo creationPropertiesProperty)
                {
                    object creationProperties = creationPropertiesProperty.GetValue(webView2);

                    if (creationProperties?.GetType().GetProperty("AdditionalBrowserArguments", BindingFlags.Public | BindingFlags.Instance) is PropertyInfo additionalBrowserArgumentsProperty)
                    {
                        string additionalBrowserArguments = (additionalBrowserArgumentsProperty.GetValue(creationProperties) as string) ?? string.Empty;
                        additionalBrowserArgumentsProperty.SetValue(creationProperties, additionalBrowserArguments + "--enable-features=WebContentsForceDark");
                    }
                }
            }
        }
#endif
    }

    public override void Preview(string path)
    {
        FallbackPath = Path.GetDirectoryName(path);
        _dotContent = File.ReadAllText(path);
        _homePage = _resources["/gv2html.html"];
        NavigateToUri(new Uri("file://quicklook/"));
    }

    protected override void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        base.WebView_CoreWebView2InitializationCompleted(sender, e);
        if (e.IsSuccess)
            _webView.NavigationCompleted += GvView_NavigationCompleted;
    }

    private void GvView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess || _dotContent == null)
            return;

        var jsonString = EscapeJsonString(_dotContent);
        var script = $"(function(){{var d={jsonString};function t(){{if(typeof window.__GV2HTML_RENDER==='function'){{window.__GV2HTML_RENDER(d);}}else{{setTimeout(t,10);}}}}t();}})();";
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
