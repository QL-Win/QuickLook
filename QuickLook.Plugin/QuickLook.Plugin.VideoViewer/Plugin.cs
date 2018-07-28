// Copyright © 2017 Paddy Xu
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
using System.IO;
using System.Linq;
using System.Windows;
using QuickLook.Common.Plugin;
using TagLib;
using File = TagLib.File;

namespace QuickLook.Plugin.VideoViewer
{
    public class Plugin : IViewer
    {
        private static readonly string[] Formats =
        {
            // video
            ".3g2", ".3gp", ".3gp2", ".3gpp", ".amv", ".asf", ".asf", ".avi", ".flv", ".mts", ".m2ts", ".m4v", ".mkv",
            ".mov", ".mp4", ".mp4v", ".mpeg", ".mpg", ".ogv", ".qt", ".tp", ".ts", ".vob", ".webm", ".wmv",
            // audio
            ".3gp", ".aa", ".aac", ".aax", ".act", ".aiff", ".amr", ".ape", ".au", ".awb", ".dct", ".dss", ".dvf",
            ".flac", ".gsm", ".iklax", ".ivs", ".m4a", ".m4b", ".m4p", ".mmf", ".mp3", ".mpc", ".msv", ".ogg",
            ".oga", ".mogg", ".opus", ".ra", ".rm", ".raw", ".tta", ".vox", ".wav", ".wma", ".wv", ".webm"
        };

        private ContextObject _context;
        private File _det;

        private ViewerPanel _vp;

        public int Priority => 0 - 10; // make it lower than TextViewer

        public void Init()
        {
            QLVRegistry.Register();
        }

        public bool CanHandle(string path)
        {
            return !Directory.Exists(path) && Formats.Contains(Path.GetExtension(path)?.ToLower());
        }

        public void Prepare(string path, ContextObject context)
        {
            _context = context;

            try
            {
                _det = File.Create(path);
            }
            catch (Exception)
            {
                // ignored
            }

            context.TitlebarOverlap = true;

            if ((_det?.Properties.MediaTypes ?? MediaTypes.Video).HasFlag(MediaTypes.Video)) // video
            {
                var windowSize = new Size
                {
                    Width = Math.Max(1366, _det?.Properties.VideoWidth ?? 1366),
                    Height = Math.Max(768, _det?.Properties.VideoHeight ?? 768)
                };
                context.SetPreferredSizeFit(windowSize, 0.8);

                context.TitlebarAutoHide = true;
                context.Theme = Themes.Dark;
                context.TitlebarBlurVisibility = true;
            }
            else // audio
            {
                context.PreferredSize = new Size(500, 300);

                context.CanResize = false;
                context.TitlebarAutoHide = false;
                context.TitlebarBlurVisibility = false;
                context.TitlebarColourVisibility = false;
            }
        }

        public void View(string path, ContextObject context)
        {
            _vp = new ViewerPanel(context);

            context.ViewerContent = _vp;

            context.Title = $"{Path.GetFileName(path)}";

            _vp.LoadAndPlay(path, _det);
        }

        public void Cleanup()
        {
            _vp?.Dispose();
            _vp = null;

            _det?.Dispose();
            _det = null;

            _context = null;
        }
    }
}