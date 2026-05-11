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
using QuickLook.Plugin.ImageViewer.Webview.Svg;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using System.Windows;

namespace QuickLook.Plugin.ImageViewer.Webview.PlantUml;

public class PuImagePanel : SvgImagePanel
{
    private string _puContent;

    public PuImagePanel()
    {
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
    }

    public override void Preview(string path)
    {
        bool consented = SettingHelper.Get("PlantUMLOnlineConsented", false, "QuickLook.Plugin.ImageViewer");

        if (!consented)
        {
            string translationFile = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Translations.config");

            string title = TranslationHelper.Get("PU_OnlineConsentTitle", translationFile, failsafe: "PlantUML Online Service");
            string message = TranslationHelper.Get("PU_OnlineConsentMessage", translationFile, failsafe:
                "Previewing this file requires sending its content to a remote PlantUML server (plantuml.com) for rendering.\n\nAllow network access?");

            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                string deniedMsg = TranslationHelper.Get("PU_OnlineDeniedMessage", translationFile, failsafe:
                    "Network access was denied. PlantUML preview requires an internet connection.");
                NavigateToHtml(
                    $"<html><body style='font-family:sans-serif;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;color:#555'>" +
                    $"<p style='max-width:400px;text-align:center'>{HttpUtility.HtmlEncode(deniedMsg)}</p></body></html>");
                return;
            }

            SettingHelper.Set("PlantUMLOnlineConsented", true, "QuickLook.Plugin.ImageViewer");
        }

        FallbackPath = Path.GetDirectoryName(path);
        _puContent = File.ReadAllText(path);
        _homePage = _resources["/pu2html.html"];
        NavigateToUri(new Uri("file://quicklook/"));
    }

    protected override void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        base.WebView_CoreWebView2InitializationCompleted(sender, e);
        if (e.IsSuccess)
            _webView.NavigationCompleted += PuView_NavigationCompleted;
    }

    private void PuView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess || _puContent == null)
            return;

        var jsonString = EscapeJsonString(_puContent);
        var script = $"(function(){{var t={jsonString};function p(){{if(typeof window.__PU2HTML_RENDER==='function'){{window.__PU2HTML_RENDER(t);}}else{{setTimeout(p,10);}}}}p();}})();";
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
