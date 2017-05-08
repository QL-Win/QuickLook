using System;
using System.IO;
using System.Linq;
using System.Windows;
using Unosquare.FFmpegMediaElement;

namespace QuickLook.Plugin.VideoViewer
{
    public class PluginInterface : IViewer
    {
        private ViewerPanel _vp;

        public int Priority => int.MaxValue;

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
            using (var element = new MediaElement {Source = new Uri(path)})
            {
                context.SetPreferredSizeFit(new Size(element.NaturalVideoWidth, element.NaturalVideoHeight), 0.6);
                context.PreferredSize = new Size(context.PreferredSize.Width,
                    context.PreferredSize.Height + 26); // add control bar
            }
        }

        public void View(string path, ContextObject context)
        {
            _vp = new ViewerPanel();

            context.ViewerContent = _vp;

            _vp.LoadAndPlay(path);

            context.Title =
                $"{Path.GetFileName(path)} ({_vp.mediaElement.NaturalVideoWidth}×{_vp.mediaElement.NaturalVideoHeight})";
            context.IsBusy = false;
        }

        public void Dispose()
        {
            _vp?.Dispose();
        }
    }
}