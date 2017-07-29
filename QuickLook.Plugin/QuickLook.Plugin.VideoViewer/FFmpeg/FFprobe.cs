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

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml.XPath;

namespace QuickLook.Plugin.VideoViewer.FFmpeg
{
    internal class FFprobe
    {
        private static readonly string _probePath =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "FFmpeg\\",
                App.Is64Bit ? "x64\\" : "x86\\", "ffprobe.exe");

        private XPathNavigator infoNavigator;

        public FFprobe(string media)
        {
            Run(media);
        }

        private bool Run(string media)
        {
            var result = string.Empty;

            using (var p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = _probePath;
                p.StartInfo.Arguments = $"-v quiet -print_format xml -show_streams -show_format \"{media}\"";
                p.Start();
                p.WaitForExit();

                result = p.StandardOutput.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(result))
                return false;

            ParseResult(result);
            return true;
        }

        private void ParseResult(string result)
        {
            infoNavigator = new XPathDocument(new StringReader(result)).CreateNavigator();
        }

        public bool CanDecode()
        {
            var info = infoNavigator.SelectSingleNode("/ffprobe/streams");

            return info != null;
        }

        public string GetFormatName()
        {
            var format = infoNavigator.SelectSingleNode("/ffprobe/format/@format_name")?.Value;

            return format ?? string.Empty;
        }

        public string GetFormatLongName()
        {
            var format = infoNavigator.SelectSingleNode("/ffprobe/format/@format_long_name")?.Value;

            return format ?? string.Empty;
        }

        public Size GetViewSize()
        {
            var width = infoNavigator.SelectSingleNode("/ffprobe/streams/stream[@codec_type='video'][1]/@coded_width")
                ?.Value;
            var height = infoNavigator.SelectSingleNode("/ffprobe/streams/stream[@codec_type='video'][1]/@coded_height")
                ?.Value;

            if (width == null || height == null)
                return Size.Empty;

            return new Size(double.Parse(width), double.Parse(height));
        }
    }
}