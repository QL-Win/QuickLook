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
using System.Windows.Documents;
using System.Xml.XPath;

namespace QuickLook.Plugin.VideoViewer.FFmpeg
{
    internal class FFprobe
    {
        private string _rawResult;
        public string RawResult
        {
            get => _rawResult;
            internal set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _rawResult = "";
                }
                else
                {
                    _rawResult = value;
                }
            }
        }

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
                {
                    return false;
                }

                ParseResult(result);
                RawResult = XMLResultToText();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string XMLResultToText()
        {
            string res = "";
            try
            {
                XPathNavigator format = _infoNavigator.SelectSingleNode("/ffprobe/format");
                res += "Format\n\n";
                res += String.Format("{0,-30}: {1,-255}", "Filename", format.GetAttribute("filename", "").ToString());
                res += "\n";
                res += String.Format("{0,-30}: {1,-255}", "Format", format.GetAttribute("format_long_name", "").ToString());
                res += "\n";
                res += String.Format("{0,-30}: {1,-255}", "Duration", MicrosecondStringToTime(format.GetAttribute("duration", "").ToString()));
                res += "\n";
                res += String.Format("{0,-30}: {1,-255}", "Size", format.GetAttribute("size", "").ToString());
                res += "\n";
                res += String.Format("{0,-30}: {1,-255}", "Bitrate", BitrateStringToMBit(format.GetAttribute("bit_rate", "").ToString()));
                res += "\n";
                try
                {
                    res += String.Format("{0,-30}: {1,-255}", "Encoder", _infoNavigator?.SelectSingleNode("/ffprobe/format/tag[@key='encoder']/@value").ToString());
                    res += "\n";
                }
                catch { }

                XPathNodeIterator streamsIterator = _infoNavigator.Select("/ffprobe/streams/stream");
                if (streamsIterator.Count > 0)
                {
                    foreach (XPathNavigator stream in streamsIterator)
                    {
                        res += "\n";
                        string streamType = stream?.GetAttribute("codec_type", "").ToString();
                        res += String.Format("Stream {0} ({1})", stream?.GetAttribute("index", "").ToString(), streamType);
                        res += "\n";
                        res += "\n";
                        res += String.Format("{0,-30}: {1,-255}", "Codec", stream?.GetAttribute("codec_name", "").ToString());
                        res += "\n";
                        res += String.Format("{0,-30}: {1,-255}", "Codec (Long)", stream?.GetAttribute("codec_long_name", ""));
                        res += "\n";
                        res += String.Format("{0,-30}: {1,-255}", "Profile", stream?.GetAttribute("profile", ""));
                        res += "\n";
                        res += String.Format("{0,-30}: {1,-255}", "Codec Timebase", stream?.GetAttribute("codec_time_base", ""));
                        res += "\n";
                        res += String.Format("{0,-30}: {1,-255}", "Codec Tag", stream?.GetAttribute("codec_tag_string", ""));
                        res += "\n";
                        if (streamType == "audio")
                        {
                            res += String.Format("{0,-30}: {1,-255}", "Sample Format", stream?.GetAttribute("sample_fmt", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Sample Rate", stream?.GetAttribute("sample_rate", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Channels", stream?.GetAttribute("channels", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Channel Layout", stream?.GetAttribute("channel_layout", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Bits per sample", stream?.GetAttribute("bits_per_sample", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Duration", MicrosecondStringToTime(stream?.GetAttribute("duration", "")));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Bitrate", BitrateStringToMBit(stream?.GetAttribute("bit_rate", "")));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Max Bitrate", BitrateStringToMBit(stream?.GetAttribute("max_bit_rate", "")));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Frames", stream?.GetAttribute("nb_frames", ""));
                            res += "\n";
                        }
                        if (streamType == "video")
                        {
                            res += String.Format("{0,-30}: {1,-255}", "Width x Height", stream?.GetAttribute("coded_width", "") + " x " + stream?.GetAttribute("coded_height", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Aspect Ratio (Sample)", stream?.GetAttribute("sample_aspect_ratio", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Aspect Ration (Display)", stream?.GetAttribute("display_aspect_ratio", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Pix Format", stream?.GetAttribute("pix_fmt", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Level", stream?.GetAttribute("level", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Framerate", stream?.GetAttribute("r_frame_rate", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Framerate (avg)", stream?.GetAttribute("avg_frame_rate", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Timebase", stream?.GetAttribute("time_base", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Duration", MicrosecondStringToTime(stream?.GetAttribute("duration", "")));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Bitrate", BitrateStringToMBit(stream?.GetAttribute("bit_rate", "")));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Bits per sample", stream?.GetAttribute("bits_per_raw_sample", ""));
                            res += "\n";
                            res += String.Format("{0,-30}: {1,-255}", "Frames", stream?.GetAttribute("nb_frames", ""));
                            res += "\n";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                res += ex.Message;
                return res;
            }
            return res;
        }

        private string MicrosecondStringToTime(string ms)
        {
            try
            {
                double d = Double.Parse(ms);
                TimeSpan t = TimeSpan.FromMilliseconds(d * 1000); // 
                return t.ToString();
            }
            catch
            {
                return "";
            }
        }

        private string BitrateStringToMBit(string bitrate)
        {
            try
            {
                double d = Double.Parse(bitrate);
                string mbps = (d / 1024.0 / 1024.0).ToString("N2");
                return String.Format("{0} MBps ({1})", mbps, bitrate);
            }
            catch
            {
                return "";
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