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
        private ViewerPanel _vp;

        public int Priority => int.MaxValue;
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

            var formats = new[]
            {
                ".3g2", ".3gp", ".3gp2", ".3gpp", ".amv", ".asf", ".asf", ".avi", ".flv", ".m2ts", ".m4v", ".mkv",
                ".mov", ".mp4", ".mp4v", ".mpeg", ".mpg", ".ogv", ".qt", ".vob", ".webm", ".wmv"
            };

            if (formats.Contains(Path.GetExtension(path).ToLower()))
                return true;

            return false;
        }

        public void Prepare(string path, ContextObject context)
        {
            var def = new Size(1024, 768);

            var real = new FFprobe(path).GetViewSize();

            context.SetPreferredSizeFit(real == Size.Empty ? def : real, 0.6);
        }

        public void View(string path, ContextObject context)
        {
            _vp = new ViewerPanel(context);

            context.ViewerContent = _vp;

            _vp.LoadAndPlay(path);

            context.Title =
                $"{Path.GetFileName(path)} ({_vp.mediaElement.NaturalVideoWidth}×{_vp.mediaElement.NaturalVideoHeight})";
            context.IsBusy = false;
        }

        public void Cleanup()
        {
            _vp?.Dispose();
            _vp = null;
        }
    }
}