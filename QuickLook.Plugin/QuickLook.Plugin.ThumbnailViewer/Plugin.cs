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

using QuickLook.Common.Plugin;
using QuickLook.Plugin.ImageViewer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ThumbnailViewer;

public class Plugin : IViewer
{
    private static readonly HashSet<string> WellKnownExtensions = new(
    [
        ".cdr", // CorelDraw
        ".fig", // Figma
        ".kra", // Krita
        ".pdn", // Paint.NET
        ".pip", ".pix", // Pixso
        ".sketch", // Sketch
        ".xd", // AdobeXD
        ".xmind", // XMind
    ]);

    private ImagePanel _ip;

    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path) && WellKnownExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    public void Prepare(string path, ContextObject context)
    {
        Handler.Prepare(path, context);
    }

    public void View(string path, ContextObject context)
    {
        _ip = new ImagePanel
        {
            ContextObject = context,
            SaveAsVisibility = Visibility.Visible,
            ReverseColorVisibility = Visibility.Visible,
            MetaIconVisibility = Visibility.Collapsed,
        };

        _ = Task.Run(() =>
        {
            using Stream imageData = Handler.ViewImage(path);

            if (imageData is null) return;

            BitmapImage bitmap = imageData.ReadAsBitmapImage();

            if (_ip is null) return;

            _ip.Dispatcher.Invoke(() =>
            {
                _ip.Source = bitmap;
                _ip.DoZoomToFit();
            });
            context.IsBusy = false;
            context.Title = $"{bitmap.PixelWidth}x{bitmap.PixelHeight}: {Path.GetFileName(path)}";
        });

        context.ViewerContent = _ip;
        context.Title = $"{Path.GetFileName(path)}";
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _ip = null;
    }
}
