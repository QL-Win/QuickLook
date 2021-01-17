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
        private ContextObject _context;
        private MediaInfo.MediaInfo _mediaInfo;

        private ViewerPanel _vp;

        public int Priority => -3;

        public void Init()
        {
            QLVRegistry.Register();
        }

        private MediaInfo.MediaInfo MediaInfoObj()
        {
            if (_mediaInfo == null)
            {
                _mediaInfo = new MediaInfo.MediaInfo(Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    Environment.Is64BitProcess ? "MediaInfo-x64\\" : "MediaInfo-x86\\"));
            }
            return _mediaInfo;
        }

        private void ResetMediaInfoObj()
        {
            _mediaInfo?.Dispose();
            _mediaInfo = null;
        }

        public bool CanHandle(string path)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    var mediaInfo = MediaInfoObj();
                    mediaInfo.Open(path);
                    string videoCodec = mediaInfo.Get(StreamKind.Video, 0, "Format");
                    string audioCodec = mediaInfo.Get(StreamKind.Audio, 0, "Format");
                    ResetMediaInfoObj();
                    if (videoCodec == "Unable to load MediaInfo library")
                    {
                        return false;
                    }
                    if (!string.IsNullOrWhiteSpace(videoCodec) || !string.IsNullOrWhiteSpace(audioCodec))
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    ResetMediaInfoObj();
                }
            }

            return false;
        }

        public void Prepare(string path, ContextObject context)
        {
            _context = context;

            try
            {
                var mediaInfo = MediaInfoObj();
                mediaInfo.Option("Cover_Data", "base64");

                mediaInfo.Open(path);
                string videoCodec = mediaInfo.Get(StreamKind.Video, 0, "Format");
                if (!string.IsNullOrWhiteSpace(videoCodec))  // video
                {
                    int.TryParse(mediaInfo?.Get(StreamKind.Video, 0, "Width"), out var width);
                    int.TryParse(mediaInfo?.Get(StreamKind.Video, 0, "Height"), out var height);
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
            catch (Exception)
            {
                ResetMediaInfoObj();
            }

            context.TitlebarOverlap = true;
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

            ResetMediaInfoObj();
            _context = null;
        }
    }
}
