﻿// Copyright © 2018 Paddy Xu
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

namespace QuickLook.Plugin.ImageViewer
{
    public class Plugin : IViewer
    {
        private static readonly HashSet<string> Formats = new HashSet<string>(new[]
        {
            ".apng", ".ari", ".arw", ".avif", ".bay", ".bmp", ".cap", ".cr2", ".cr3", ".crw", ".dcr", ".dcs", ".dng",
            ".drf", ".eip", ".emf", ".erf", ".exr", ".fff", ".gif", ".hdr", ".heic", ".heif", ".ico", ".icon", ".iiq",
            ".jfif", ".jpeg", ".jpg", ".k25", ".kdc", ".mdc", ".mef", ".mos", ".mrw", ".nef", ".nrw", ".obm", ".orf",
            ".pbm", ".pef", ".pgm", ".png", ".pnm", ".ppm", ".psd", ".ptx", ".pxn", ".r3d", ".raf", ".raw", ".rw2",
            ".rwl", ".rwz", ".sr2", ".srf", ".srw", ".svg", ".tga", ".tif", ".tiff", ".wdp", ".webp", ".wmf", ".x3f"
        });

        private ImagePanel _ip;
        private MetaProvider _meta;

        public int Priority => 0;

        public void Init()
        {
            AnimatedImage.AnimatedImage.Providers.Add(
                new KeyValuePair<string[], Type>(new[] {".apng", ".png"},
                    typeof(APngProvider)));
            AnimatedImage.AnimatedImage.Providers.Add(
                new KeyValuePair<string[], Type>(new[] {".gif"},
                    typeof(GifProvider)));
            AnimatedImage.AnimatedImage.Providers.Add(
                new KeyValuePair<string[], Type>(new[] {".bmp", ".jpg", ".jpeg", ".jfif", ".tif", ".tiff"},
                    typeof(NativeProvider)));
            AnimatedImage.AnimatedImage.Providers.Add(
                new KeyValuePair<string[], Type>(new[] {"*"},
                    typeof(ImageMagickProvider)));
        }

        public bool CanHandle(string path)
        {
            return !Directory.Exists(path) && Formats.Contains(Path.GetExtension(path.ToLower()));
        }

        public void Prepare(string path, ContextObject context)
        {
            _meta = new MetaProvider(path);

            var size = _meta.GetSize();

            if (!size.IsEmpty)
                context.SetPreferredSizeFit(size, 0.8);
            else
                context.PreferredSize = new Size(800, 600);

            context.Theme = (Themes) SettingHelper.Get("LastTheme", 1);
        }

        public void View(string path, ContextObject context)
        {
            _ip = new ImagePanel(context, _meta);
            var size = _meta.GetSize();

            context.ViewerContent = _ip;
            context.Title = size.IsEmpty
                ? $"{Path.GetFileName(path)}"
                : $"{size.Width}×{size.Height}: {Path.GetFileName(path)}";

            _ip.ImageUriSource = new Uri(path);
        }

        public void Cleanup()
        {
            _ip?.Dispose();
            _ip = null;
        }
    }
}
