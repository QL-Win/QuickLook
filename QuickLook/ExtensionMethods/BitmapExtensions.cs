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

using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickLook.ExtensionMethods
{
    public static class BitmapExtensions
    {
        public static BitmapSource ToBitmapSource(this Bitmap old_source)
        {
            // BitmapSource.Create throws an exception when the image is scanned backward.
            // The Clone() will make it back scanning forward.
            var source = (Bitmap) old_source.Clone();

            BitmapSource bs = null;
            try
            {
                var data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height),
                    ImageLockMode.ReadOnly, source.PixelFormat);

                bs = BitmapSource.Create(source.Width, source.Height, source.HorizontalResolution,
                    source.VerticalResolution, PixelFormats.Bgra32, null,
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
                source.Dispose();
            }

            return bs;
        }
    }
}