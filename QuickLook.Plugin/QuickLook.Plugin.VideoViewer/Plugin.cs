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
using Meta.Vlc;
using Meta.Vlc.Interop.Media;
using Meta.Vlc.Wpf;
using QuickLook.Common;
using Size = System.Windows.Size;
using VideoTrack = Meta.Vlc.VideoTrack;

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

        private ViewerPanel _vp;

        public int Priority => 0 - 10; // make it lower than TextViewer

        public void Init()
        {
            ApiManager.Initialize(VlcSettings.LibVlcPath, VlcSettings.VlcOptions);
        }

        public bool CanHandle(string path)
        {
            return !Directory.Exists(path) && Formats.Contains(Path.GetExtension(path)?.ToLower());
        }

        public void Prepare(string path, ContextObject context)
        {
            _context = context;

            var def = new Size(500, 300);

            var hasVideo = HasVideoStream(path, out var mediaSize);

            var windowSize = mediaSize == Size.Empty ? def : mediaSize;
            windowSize.Width = Math.Max(def.Width, windowSize.Width);
            windowSize.Height = Math.Max(def.Height, windowSize.Height);

            context.SetPreferredSizeFit(windowSize, 0.8);
            context.TitlebarOverlap = true;
            if (!hasVideo)
            {
                context.CanResize = false;
                context.TitlebarAutoHide = false;
                context.TitlebarBlurVisibility = false;
                context.TitlebarColourVisibility = false;
            }
            else
            {
                context.TitlebarAutoHide = true;
                context.UseDarkTheme = true;
                context.TitlebarBlurVisibility = true;
            }
        }

        public void View(string path, ContextObject context)
        {
            _vp = new ViewerPanel(context);

            context.ViewerContent = _vp;

            _vp.mediaElement.VlcMediaPlayer.Opening += MarkReady;

            _vp.LoadAndPlay(path);

            context.Title = $"{Path.GetFileName(path)}";
        }

        public void Cleanup()
        {
            if (_vp != null)
                _vp.mediaElement.VlcMediaPlayer.Opening -= MarkReady;
            _vp?.Dispose();
            _vp = null;

            _context = null;
        }

        private void MarkReady(object sender, ObjectEventArgs<MediaState> e)
        {
            _context.IsBusy = false;
        }

        private static bool HasVideoStream(string path, out Size size)
        {
            using (var vlc = new Vlc(VlcSettings.VlcOptions))
            {
                using (var media = vlc.CreateMediaFromPath(path))
                {
                    media.Parse();
                    var tracks = media.GetTracks();
                    var video = tracks.FirstOrDefault(mt => mt.Type == TrackType.Video) as VideoTrack;

                    size = video == null ? Size.Empty : new Size(video.Width, video.Height);

                    return video != null;
                }
            }
        }
    }
}