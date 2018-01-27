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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Windows.Media.PixelFormat;

namespace QuickLook.Common.ExtensionMethods
{
    public static class BitmapExtensions
    {
        public static BitmapSource ToBitmapSource(this Bitmap source)
        {
            var orgSource = source;
            BitmapSource bs = null;
            try
            {
                var data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height),
                    ImageLockMode.ReadOnly, source.PixelFormat);

                // BitmapSource.Create throws an exception when the image is scanned backward.
                // The Clone() will make it back scanning forward.
                if (data.Stride < 0)
                {
                    source.UnlockBits(data);
                    source = (Bitmap) source.Clone();
                    data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height),
                        ImageLockMode.ReadOnly, source.PixelFormat);
                }

                bs = BitmapSource.Create(source.Width, source.Height, Math.Floor(source.HorizontalResolution),
                    Math.Floor(source.VerticalResolution), ConvertPixelFormat(source.PixelFormat), null,
                    data.Scan0, data.Stride * source.Height, data.Stride);

                source.UnlockBits(data);

                bs.Freeze();
            }
            catch
            {
                // ignored
            }
            finally
            {
                if (orgSource != source)
                    source.Dispose();
            }

            return bs;
        }

        public static Bitmap ToBitmap(this BitmapSource source)
        {
            using (var outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(source));
                enc.Save(outStream);
                var bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }

        private static PixelFormat ConvertPixelFormat(
            System.Drawing.Imaging.PixelFormat sourceFormat)
        {
            switch (sourceFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    return PixelFormats.Bgr24;

                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    return PixelFormats.Bgra32;

                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    return PixelFormats.Bgr32;
            }

            return new PixelFormat();
        }

        public static bool IsDarkImage(this Bitmap image)
        {
            // convert to 24-bit RGB image
            image = image.Clone(new Rectangle(0, 0, image.Width, image.Height),
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            var sampleCount = (int) (0.2 * 400 * 400);
            const int pixelSize = 24 / 8;
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadWrite, image.PixelFormat);

            var darks = 0;
            unsafe
            {
                var pFirst = (byte*) data.Scan0;

                Parallel.For(0, sampleCount, n =>
                {
                    var rand = new Random(n);
                    var row = rand.Next(0, data.Height);
                    var col = rand.Next(0, data.Width);
                    var pos = pFirst + row * data.Stride + col * pixelSize;

                    var b = pos[0];
                    var g = pos[1];
                    var r = pos[2];

                    var y = (0.299 * r + 0.587 * g + 0.114 * b) / 255;

                    if (y < 0.5)
                        darks++;
                });
            }

            image.UnlockBits(data);
            image.Dispose();

            return darks > 0.65 * sampleCount;
        }

        public static BitmapImage LoadBitmapImage(this Uri source)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = source;
            bitmap.EndInit();
            return bitmap;
        }
    }
}