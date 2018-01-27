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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageMagick;
using QuickLook.Common.ExtensionMethods;

namespace QuickLook.Plugin.ImageViewer.Exiv2
{
    public class Meta
    {
        private static readonly string ExivPath =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "exiv2\\exiv2.exe");
        private readonly string _path;

        private OrientationType _orientation = OrientationType.Undefined;
        private Dictionary<string, string> _summary;

        public Meta(string path)
        {
            _path = path;
        }

        public Dictionary<string, string> GetSummary()
        {
            if (_summary != null)
                return _summary;

            return _summary = Run($"\"{_path}\"", ": ");
        }

        public BitmapSource GetThumbnail(bool autoZoom = false)
        {
            GetOrientation();

            var count = Run($"-pp \"{_path}\"", ",").Count;

            if (count == 0)
                return null;

            var suc = Run($"-f -ep{count} -l \"{Path.GetTempPath().TrimEnd('\\')}\" \"{_path}\"", ",");
            if (suc.Count != 0)
                return null;

            try
            {
                using (var image = new MagickImage(Path.Combine(Path.GetTempPath(),
                    $"{Path.GetFileNameWithoutExtension(_path)}-preview{count}.jpg")))
                {
                    File.Delete(image.FileName);

                    if (_orientation == OrientationType.RightTop)
                        image.Rotate(90);
                    else if (_orientation == OrientationType.BottomRight)
                        image.Rotate(180);
                    else if (_orientation == OrientationType.LeftBotom)
                        image.Rotate(270);
                    if (!autoZoom)
                        return image.ToBitmapSource();

                    var size = GetSize();
                    return new TransformedBitmap(image.ToBitmapSource(),
                        new ScaleTransform(size.Width / image.Width, size.Height / image.Height));
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Size GetSize()
        {
            if (_summary == null)
                GetSummary();

            if (!_summary.ContainsKey("Image size"))
                return Size.Empty;

            var width = int.Parse(_summary["Image size"].Split('x')[0].Trim());
            var height = int.Parse(_summary["Image size"].Split('x')[1].Trim());

            switch (GetOrientation())
            {
                case OrientationType.RightTop:
                case OrientationType.LeftBotom:
                    return new Size(height, width);
                default:
                    return new Size(width, height);
            }
        }

        public OrientationType GetOrientation()
        {
            if (_orientation != OrientationType.Undefined)
                return _orientation;

            try
            {
                var ori = Run($"-g Exif.Image.Orientation -Pkv \"{_path}\"", "\\s{1,}");

                if (ori?.ContainsKey("Exif.Image.Orientation") == true)
                    _orientation = (OrientationType) int.Parse(ori["Exif.Image.Orientation"]);
                else
                    _orientation = OrientationType.TopLeft;
            }
            catch (Exception)
            {
                _orientation = OrientationType.TopLeft;
            }

            return _orientation;
        }

        private Dictionary<string, string> Run(string arg, string regexSplit)
        {
            try
            {
                string result;
                using (var p = new Process())
                {
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.FileName = ExivPath;
                    p.StartInfo.Arguments = arg;
                    p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    p.Start();

                    result = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(1000);
                }

                return string.IsNullOrWhiteSpace(result)
                    ? new Dictionary<string, string>()
                    : ParseResult(result, regexSplit);
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }
        }

        private Dictionary<string, string> ParseResult(string result, string regexSplit)
        {
            var res = new Dictionary<string, string>();

            result.Replace("\r\n", "\n").Split('\n').ForEach(l =>
            {
                if (string.IsNullOrWhiteSpace(l))
                    return;
                var eles = Regex.Split(l, regexSplit + "");
                res.Add(eles[0].Trim(), eles.Skip(1).Aggregate((a, b) => $"{a}{regexSplit}{b}"));
            });

            return res;
        }
    }
}