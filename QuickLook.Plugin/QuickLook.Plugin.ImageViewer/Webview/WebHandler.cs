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

using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.ImageViewer.Webview.Lottie;
using QuickLook.Plugin.ImageViewer.Webview.Svg;
using QuickLook.Plugin.ImageViewer.Webview.Svga;
using System;
using System.IO;
using System.Windows;

namespace QuickLook.Plugin.ImageViewer.Webview;

internal static class WebHandler
{
    public static bool TryCanHandle(string path)
    {
        if (path.EndsWith(".lottie.json", StringComparison.OrdinalIgnoreCase))
            return true;

        return Path.GetExtension(path).ToLower() switch
        {
            ".svg" => SettingHelper.Get("RenderSvgWeb", true, "QuickLook.Plugin.ImageViewer"),
            ".svga" or ".lottie" => true,
            ".json" => LottieDetector.IsVaild(path), // Check for Lottie files
            _ => false,
        };
    }

    public static bool TryPrepare(string path, ContextObject context, out IWebMetaProvider metaWeb)
    {
        string ext = Path.GetExtension(path).ToLower();

        if (ext == ".svg" || ext == ".svga"
         || ext == ".lottie" || ext == ".json")
        {
            if (ext == ".svg")
            {
                if (!SettingHelper.Get("RenderSvgWeb", true, "QuickLook.Plugin.ImageViewer"))
                {
                    metaWeb = null;
                    return false;
                }
            }

            metaWeb = ext switch
            {
                ".svg" => new SvgMetaProvider(path),
                ".svga" => new SvgaMetaProvider(path),
                ".lottie" or ".json" => new LottieMetaProvider(path),
                _ => throw new NotSupportedException($"Unsupported file type: {ext}")
            };
            var sizeSvg = metaWeb.GetSize();

            if (!sizeSvg.IsEmpty)
                context.SetPreferredSizeFit(sizeSvg, 0.8d);
            else
                context.PreferredSize = new Size(800, 600);

            context.Theme = (Themes)SettingHelper.Get("LastTheme", 1, "QuickLook.Plugin.ImageViewer");
            return true;
        }

        metaWeb = null;
        return false;
    }

    public static bool TryView(string path, ContextObject context, IWebMetaProvider metaWeb, out IWebImagePanel ipWeb)
    {
        string ext = Path.GetExtension(path).ToLower();

        if (ext == ".svg" || ext == ".svga"
         || ext == ".lottie" || ext == ".json")
        {
            if (ext == ".svg")
            {
                if (!SettingHelper.Get("RenderSvgWeb", true, "QuickLook.Plugin.ImageViewer"))
                {
                    ipWeb = null;
                    return false;
                }
            }

            ipWeb = ext switch
            {
                ".svg" => new SvgImagePanel(),
                ".svga" => new SvgaImagePanel(metaWeb),
                ".lottie" or ".json" => new LottieImagePanel(),
                _ => throw new NotSupportedException($"Unsupported file type: {ext}")
            };

            ipWeb.Preview(path);

            var sizeSvg = metaWeb.GetSize();

            context.ViewerContent = ipWeb;
            context.Title = sizeSvg.IsEmpty
                ? $"{Path.GetFileName(path)}"
                : $"{sizeSvg.Width}×{sizeSvg.Height}: {Path.GetFileName(path)}";

            context.IsBusy = false;
            return true;
        }

        ipWeb = null;
        return false;
    }
}
