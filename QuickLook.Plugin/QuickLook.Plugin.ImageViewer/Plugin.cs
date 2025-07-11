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
using QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;
using QuickLook.Plugin.ImageViewer.Webview;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace QuickLook.Plugin.ImageViewer;

public class Plugin : IViewer
{
    private static readonly HashSet<string> WellKnownExtensions = new(
    [
        ".apng", ".ari", ".arw", ".avif", ".ani",
        ".bay", ".bmp",
        ".cap", ".cr2", ".cr3", ".crw", ".cur",
        ".dcr", ".dcs", ".dds", ".dng", ".drf",
        ".eip", ".emf", ".erf", ".exr",
        ".fff",
        ".gif",
        ".hdr", ".heic", ".heif",
        ".ico", ".icon", ".icns", ".iiq",
        ".jfif", ".jp2", ".jpeg", ".jpg", ".jxl", ".j2k", ".jpf", ".jpx", ".jpm", ".jxr",
        ".k25", ".kdc",
        ".mdc", ".mef", ".mos", ".mrw", ".mj2", ".miff",
        ".nef", ".nrw",
        ".obm", ".orf",
        ".pbm", ".pcx", ".pef", ".pgm", ".png", ".pnm", ".ppm", ".psb", ".psd", ".ptx", ".pxn",
        ".qoi",
        ".r3d", ".raf", ".raw", ".rw2", ".rwl", ".rwz",
        ".sr2", ".srf", ".srw", ".svg", ".svgz",
        ".tga", ".tif", ".tiff",
        ".wdp", ".webp", ".wmf",
        ".x3f", ".xcf", ".xbm", ".xpm",
    ]);

    private ImagePanel _ip;
    private MetaProvider _meta;

    private IWebImagePanel _ipWeb;
    private IWebMetaProvider _metaWeb;

    public int Priority => 0;

    public void Init()
    {
        var useColorProfile = SettingHelper.Get("UseColorProfile", false, "QuickLook.Plugin.ImageViewer");

        AnimatedImage.AnimatedImage.Providers.Add(
            new KeyValuePair<string[], Type>(
                useColorProfile ? [".apng"] : [".apng", ".png"],
                typeof(APngProvider)));
        AnimatedImage.AnimatedImage.Providers.Add(
            new KeyValuePair<string[], Type>([".gif"],
                typeof(GifProvider)));
        AnimatedImage.AnimatedImage.Providers.Add(
            new KeyValuePair<string[], Type>(
                useColorProfile ? [] : [".bmp", ".jpg", ".jpeg", ".jfif", ".tif", ".tiff"],
                typeof(NativeProvider)));
        AnimatedImage.AnimatedImage.Providers.Add(
            new KeyValuePair<string[], Type>(
                useColorProfile ? [] : [".jxr"],
                typeof(WmpProvider)));
        AnimatedImage.AnimatedImage.Providers.Add(
            new KeyValuePair<string[], Type>([".icns"],
                typeof(IcnsProvider)));
        AnimatedImage.AnimatedImage.Providers.Add(
            new KeyValuePair<string[], Type>([".webp"],
                typeof(WebPProvider)));
        AnimatedImage.AnimatedImage.Providers.Add(
            new KeyValuePair<string[], Type>([".cur", ".ani"],
                typeof(CursorProvider)));
#if USESVGSKIA
        AnimatedImage.AnimatedImage.Providers.Add(
            new KeyValuePair<string[], Type>([".svg"],
                typeof(SvgProvider)));
#endif
        AnimatedImage.AnimatedImage.Providers.Add(
            new KeyValuePair<string[], Type>(["*"],
                typeof(ImageMagickProvider)));
    }

    public bool CanHandle(string path)
    {
        if (WebHandler.TryCanHandle(path))
            return true;

        // Disabled due mishandling text file types e.g., "*.config".
        // Only check extension for well known image and animated image types.
        return !Directory.Exists(path) && WellKnownExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    public void Prepare(string path, ContextObject context)
    {
        if (WebHandler.TryPrepare(path, context, out _metaWeb))
            return;

        _meta = new MetaProvider(path);

        var size = _meta.GetSize();

        if (!size.IsEmpty)
            context.SetPreferredSizeFit(size, 0.8d);
        else
            context.PreferredSize = new Size(800, 600);

        context.Theme = (Themes)SettingHelper.Get("LastTheme", 1, "QuickLook.Plugin.ImageViewer");
    }

    public void View(string path, ContextObject context)
    {
        if (WebHandler.TryView(path, context, _metaWeb, out _ipWeb))
            return;

        _ip = new ImagePanel(context, _meta);
        var size = _meta.GetSize();

        context.ViewerContent = _ip;
        context.Title = size.IsEmpty
            ? $"{Path.GetFileName(path)}"
            : $"{size.Width}×{size.Height}: {Path.GetFileName(path)}";

        _ip.ImageUriSource = Helper.FilePathToFileUrl(path);

        // Load the custom cursor into the preview panel
        if (new[] { ".cur", ".ani" }.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            _ip.Cursor = CursorProvider.GetCursor(path) ?? Cursors.Arrow;
        }
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _ip?.Dispose();
        _ip = null;

        _ipWeb?.Dispose();
        _ipWeb = null;
    }
}
