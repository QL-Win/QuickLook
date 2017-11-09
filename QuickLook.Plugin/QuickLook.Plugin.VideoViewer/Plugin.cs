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
using System.Reflection;
using System.Windows;
using QuickLook.Plugin.VideoViewer.FFmpeg;
using Unosquare.FFME;

namespace QuickLook.Plugin.VideoViewer
{
    public class Plugin : IViewer
    {
        private static readonly string[] Formats =
        {
            // video
            ".3g2", ".3gp", ".3gp2", ".3gpp", ".amv", ".asf", ".asf", ".avi", ".flv", ".mts", ".m2ts", ".m4v", ".mkv",
            ".mov", ".mp4", ".mp4v", ".mpeg", ".mpg", ".ogv", ".qt", ".vob", ".webm", ".wmv",
            // audio
            ".3gp", ".aa", ".aac", ".aax", ".act", ".aiff", ".amr", ".ape", ".au", ".awb", ".dct", ".dss", ".dvf",
            ".flac", ".gsm", ".iklax", ".ivs", ".m4a", ".m4b", ".m4p", ".mmf", ".mp3", ".mpc", ".msv", ".ogg",
            ".oga", ".mogg", ".opus", ".ra", ".rm", ".raw", ".tta", ".vox", ".wav", ".wma", ".wv", ".webm"
        };

        private ContextObject _context;
        private ViewerPanel _vp;

        private FFprobe probe;

        public int Priority => 0 - 10; // make it lower than TextViewer

        public void Init()
        {
            MediaElement.FFmpegDirectory = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "FFmpeg\\",
                App.Is64Bit ? "x64\\" : "x86\\");
        }

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            return Formats.Contains(Path.GetExtension(path).ToLower());

            //FFprobe is much slower than fixed extensions
            //probe = new FFprobe(path);
            //return probe.CanDecode() & (probe.HasAudio() | probe.HasVideo());
        }

        public void Prepare(string path, ContextObject context)
        {
            _context = context;

            var def = new Size(500, 300);
            probe = probe ?? new FFprobe(path);

            if (!probe.HasVideo())
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
                context.CanResize = true;
                context.TitlebarAutoHide = true;
                context.TitlebarBlurVisibility = true;
                context.TitlebarColourVisibility = true;
            }

            var windowSize = probe.GetViewSize() == Size.Empty ? def : probe.GetViewSize();
            windowSize.Width = Math.Max(def.Width, windowSize.Width);
            windowSize.Height = Math.Max(def.Height, windowSize.Height);

            if (!probe.HasVideo())
                context.PreferredSize = def;
            else
                context.SetPreferredSizeFit(windowSize, 0.6);
            context.TitlebarOverlap = true;
        }

        public void View(string path, ContextObject context)
        {
            _vp = new ViewerPanel(context, probe.HasVideo());

            context.ViewerContent = _vp;

            _vp.mediaElement.MediaOpened += MediaElement_MediaOpened;
            _vp.LoadAndPlay(path);

            context.Title = $"{Path.GetFileName(path)}";

        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            _context.IsBusy = false;
        }

        public void Cleanup()
        {
            if (_vp?.mediaElement != null)
                _vp.mediaElement.MediaOpened -= MediaElement_MediaOpened;
            
            _vp?.Dispose();
            _vp = null;

            _context = null;
        }
    }
}