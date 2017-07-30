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
        private FFprobe _probe;
        private ViewerPanel _vp;

        public int Priority => 0 - 10; // make it lower than TextViewer
        public bool AllowsTransparency => true;

        public void Init()
        {
            MediaElement.FFmpegDirectory =
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "\\FFmpeg\\",
                    App.Is64Bit ? "x64\\" : "x86\\");
        }

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            var blacklist = new[]
            {
                // tty
                ".ans", ".art", ".asc", ".diz", ".ice", ".nfo", ".txt", ".vt",
                // ico
                ".ico", ".icon",
                // bmp_pipe
                ".bmp",
                // ass
                ".ass",
                // apng
                ".png", ".apng",
                // asterisk (pcmdec)
                ".gsm", ".sln"
            };

            if (blacklist.Contains(Path.GetExtension(path).ToLower()))
                return false;

            var probe = new FFprobe(path);
            // check if it is an image. Normal images shows "image2"
            // "dpx,jls,jpeg,jpg,ljpg,pam,pbm,pcx,pgm,pgmyuv,png,"
            // "ppm,sgi,tga,tif,tiff,jp2,j2c,xwd,sun,ras,rs,im1,im8,im24,"
            // "sunras,xbm,xface"
            if (probe.GetFormatName().ToLower() == "image2")
                return false;

            return probe.CanDecode();
        }

        public void Prepare(string path, ContextObject context)
        {
            var def = new Size(450, 450);

            _probe = new FFprobe(path);
            var mediaSize = _probe.GetViewSize();

            var windowSize = mediaSize == Size.Empty ? def : mediaSize;
            windowSize.Width = Math.Max(def.Width, windowSize.Width);
            windowSize.Height = Math.Max(def.Height, windowSize.Height);

            context.SetPreferredSizeFit(windowSize, 0.6);
        }

        public void View(string path, ContextObject context)
        {
            _vp = new ViewerPanel(context);

            context.ViewerContent = _vp;

            _vp.LoadAndPlay(path);

            _vp.mediaElement.MediaOpened += (sender, e) => context.IsBusy = false;

            var mediaSize = _probe.GetViewSize();
            var info = mediaSize == Size.Empty ? "Audio" : $"{mediaSize.Width}×{mediaSize.Height}";

            context.Title =
                $"{Path.GetFileName(path)} ({info})";
        }

        public void Cleanup()
        {
            _probe = null;

            _vp?.Dispose();
            _vp = null;
        }
    }
}