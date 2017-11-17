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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Xml.XPath;

namespace QuickLook.Plugin.VideoViewer.FFmpeg
{
    internal class FFprobe
    {
        private static readonly string _probePath =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "FFmpeg\\",
                App.Is64Bit ? "x64\\" : "x86\\", "ffprobe.exe");

        private XPathNavigator _infoNavigator;

        public FFprobe(string media)
        {
            Run(media);
        }

        private bool Run(string media)
        {
            try
            {
                string result;

                using (var p = new Process())
                {
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.FileName = _probePath;
                    p.StartInfo.Arguments = $"-v quiet -print_format xml -show_streams -show_format \"{media}\"";
                    p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    p.Start();

                    result = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(1000);

                }

                if (string.IsNullOrWhiteSpace(result))
                    return false;

                ParseResult(result);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ParseResult(string result)
        {
            _infoNavigator = new XPathDocument(new StringReader(result)).CreateNavigator();
        }

        public bool CanDecode()
        {
            var info = _infoNavigator?.SelectSingleNode("/ffprobe/format[@probe_score>25]");

            return info != null;
        }

        public string GetFormatName()
        {
            var format = _infoNavigator?.SelectSingleNode("/ffprobe/format/@format_name")?.Value;

            return format ?? string.Empty;
        }

        public string GetFormatLongName()
        {
            var format = _infoNavigator?.SelectSingleNode("/ffprobe/format/@format_long_name")?.Value;

            return format ?? string.Empty;
        }

        public bool HasAudio()
        {
            var duration = _infoNavigator?.SelectSingleNode("/ffprobe/streams/stream[@codec_type='audio'][1]/@duration")
                ?.Value;

            if (duration == null)
                return false;

            double.TryParse(duration, out var d);
            return Math.Abs(d) > 0.01;
        }

        public bool HasVideo()
        {
            var fps = _infoNavigator?.SelectSingleNode("/ffprobe/streams/stream[@codec_type='video'][1]/@avg_frame_rate")
                ?.Value;

            if (fps == null)
                return false;

            return fps != "0/0";
        }

        public Size GetViewSize()
        {
            if (!HasVideo())
                return Size.Empty;

            var width = _infoNavigator?.SelectSingleNode("/ffprobe/streams/stream[@codec_type='video'][1]/@coded_width")
                ?.Value;
            var height = _infoNavigator?.SelectSingleNode("/ffprobe/streams/stream[@codec_type='video'][1]/@coded_height")
                ?.Value;

            if (width == null || height == null)
                return Size.Empty;

            return new Size(double.Parse(width), double.Parse(height));
        }
    }
}