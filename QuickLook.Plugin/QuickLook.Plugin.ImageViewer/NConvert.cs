// Copyright © 2018 Paddy Xu
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
using System.Reflection;
using System.Windows;

namespace QuickLook.Plugin.ImageViewer
{
    public enum Orientation
    {
        Undefined = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 3,
        BottomLeft = 4,
        LeftTop = 5,
        RightTop = 6,
        RightBottom = 7,
        Leftbottom = 8
    }

    public class NConvert
    {
        private static readonly string NConvertPath =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NConvert\\nconvert.exe");
        private readonly List<Tuple<string, string>> _metaBasic = new List<Tuple<string, string>>();
        private readonly List<Tuple<string, string>> _metaExif = new List<Tuple<string, string>>();
        private readonly string _path;

        public NConvert(string path)
        {
            _path = path;

            GetMeta();
        }

        public List<Tuple<string, string>> GetExif()
        {
            return _metaExif;
        }

        public MemoryStream GetTiffStream(bool thumbnail)
        {
            var temp = Path.GetTempFileName();
            File.Delete(temp);

            var sony = Path.GetExtension(_path)?.ToLower() == ".arw" ? "-autolevels" : "";
            var thumb = thumbnail ? "-embedded_jpeg" : "";
            var d = RunInternal(
                $"-quiet {thumb} {sony} -raw_camerabalance -raw_autobright -icc -out tiff -o \"{temp}\" \"{_path}\"",
                10000);

            var ms = new MemoryStream(File.ReadAllBytes(temp));

            File.Delete(temp);

            return ms;
        }

        public Size GetSize()
        {
            var ws = _metaBasic.Find(t => t.Item1 == "Width")?.Item2;
            var hs = _metaBasic.Find(t => t.Item1 == "Height")?.Item2;
            int.TryParse(ws, out var w);
            int.TryParse(hs, out var h);

            if (w == 0 || h == 0)
                return Size.Empty;

            switch (GetOrientation())
            {
                case Orientation.LeftTop:
                case Orientation.RightTop:
                case Orientation.RightBottom:
                case Orientation.Leftbottom:
                    return new Size(h, w);
                default:
                    return new Size(w, h);
            }
        }

        public Orientation GetOrientation()
        {
            var o = _metaExif.Find(t => t.Item1 == "Orientation")?.Item2;
            if (!string.IsNullOrEmpty(o)) return (Orientation) int.Parse(o.Substring(o.Length - 2, 1));

            return Orientation.TopLeft;
        }

        private void GetMeta()
        {
            var lines = RunInternal($"-quiet -fullinfo \"{_path}\"")
                .Replace("\r\n", "\n")
                .Split('\n');

            var crtDict = _metaBasic;
            for (var i = 0; i < lines.Length; i++)
            {
                var segs = lines[i].Split(':');
                if (segs.Length != 2)
                    continue;

                var k = segs[0];
                var v = segs[1];

                if (k == "EXIF")
                    crtDict = _metaExif;

                if (k.StartsWith("    "))
                {
                    if (crtDict == _metaBasic) // trim all 4 spaces
                    {
                        if (!string.IsNullOrWhiteSpace(v))
                            crtDict.Add(new Tuple<string, string>(k.Trim(), v.Trim()));
                    }
                    else // in exif
                    {
                        if (!string.IsNullOrWhiteSpace(v))
                        {
                            var kk = k.Substring(0, k.Length - 8).Trim();
                            crtDict.Add(new Tuple<string, string>(kk, v.Trim())); // -8 to remove "(0xa001)"
                        }
                    }
                }
                else if (k.StartsWith("  "))
                {
                    crtDict.Add(new Tuple<string, string>(k.Trim(), string.Empty));
                }
            }
        }

        private string RunInternal(string arg, int timeout = 2000)
        {
            try
            {
                string result;
                using (var p = new Process())
                {
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.FileName = NConvertPath;
                    p.StartInfo.Arguments = arg;
                    p.Start();

                    result = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(timeout);

                    return result;
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    internal static class Extensions
    {
        public static byte[] ReadToEnd(this Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                var buffer = new byte[8192];
                int count;
                while ((count = stream.Read(buffer, 0, buffer.Length)) > 0) ms.Write(buffer, 0, count);

                return ms.ToArray();
            }
        }
    }
}