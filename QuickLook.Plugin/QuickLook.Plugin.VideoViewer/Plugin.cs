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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using MediaInfo;
using QuickLook.Common.Plugin;

namespace QuickLook.Plugin.VideoViewer
{
    public class Plugin : IViewer
    {
        private static readonly HashSet<string> Formats = new HashSet<string>(new[]
        {
            // video
            ".3g2", ".3gp", ".3gp2", ".3gpp", ".amv", ".asf", ".avi", ".flv", ".m4v", ".mkv", ".mov", ".mp4", ".mp4v", 
            ".mpeg", ".mpg", ".mts", ".m2ts", ".mxf", ".ogv", ".qt", ".tp", ".ts", ".vob", ".webm", ".wmv",
            // audio
            ".3gp", ".aa", ".aac", ".aax", ".act", ".aif", ".aiff", ".amr", ".ape", ".au", ".awb", ".dct", ".dss", ".dvf",
            ".flac", ".gsm", ".iklax", ".ivs", ".m4a", ".m4b", ".m4p", ".m4r", ".mka", ".mmf", ".mp3", ".mpc", ".msv",
            ".ogg", ".oga", ".mogg", ".opus", ".ra", ".raw", ".rm", ".tta", ".vox", ".wav", ".webm", ".wma", ".wv"
        });

        private ContextObject _context;
        private MediaInfo.MediaInfo _mediaInfo;

        private ViewerPanel _vp;

        public int Priority => -10; // make it lower than TextViewer

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
                _mediaInfo = new MediaInfo.MediaInfo(Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    Environment.Is64BitProcess ? "MediaInfo-x64\\" : "MediaInfo-x86\\"));
                _mediaInfo.Option("Cover_Data", "base64");

                _mediaInfo.Open(path);
            }
            catch (Exception)
            {
                _mediaInfo?.Dispose();
                _mediaInfo = null;
            }

            context.TitlebarOverlap = true;

            if (_mediaInfo == null ||
                !string.IsNullOrWhiteSpace(_mediaInfo.Get(StreamKind.General, 0, "VideoCount"))) // video
            {
                int.TryParse(_mediaInfo?.Get(StreamKind.Video, 0, "Width"), out var width);
                int.TryParse(_mediaInfo?.Get(StreamKind.Video, 0, "Height"), out var height);
                double.TryParse(_mediaInfo?.Get(StreamKind.Video, 0, "Rotation"), out var rotation);

                var windowSize = new Size
                {
                    Width = Math.Max(100, width == 0 ? 1366 : width),
                    Height = Math.Max(100, height == 0 ? 768 : height)
                };

                if (rotation % 180 != 0)
                    windowSize = new Size(windowSize.Height, windowSize.Width);

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

            _vp.LoadAndPlay(path, _mediaInfo);
        }

        public void Cleanup()
        {
            _vp?.Dispose();
            _vp = null;

            _mediaInfo?.Dispose();
            _mediaInfo = null;

            _context = null;
        }
    }
}
