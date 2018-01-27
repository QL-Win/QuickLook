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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PDFiumSharp;
using QuickLook.Common.Helpers;

namespace QuickLook.Plugin.PDFViewer
{
    internal static class PdfPageExtension
    {
        public static BitmapSource RenderThumbnail(this PdfPage page)
        {
            var factorX = 130d / page.Width;
            var factorY = 210d / page.Height;

            return page.Render(Math.Min(factorX, factorY), false);
        }

        public static BitmapSource Render(this PdfPage page, double factor, bool fixDpi = true)
        {
            var scale = DpiHelper.GetCurrentScaleFactor();
            var dpiX = fixDpi ? scale.Horizontal * DpiHelper.DefaultDpi : 96;
            var dpiY = fixDpi ? scale.Vertical * DpiHelper.DefaultDpi : 96;

            var realWidth = (int) Math.Round(page.Width * scale.Horizontal * factor);
            var realHeight = (int) Math.Round(page.Height * scale.Vertical * factor);

            var bitmap = new WriteableBitmap(realWidth, realHeight, dpiX, dpiY, PixelFormats.Bgr24, null);
            page.Render(bitmap,
                flags: RenderingFlags.LimitImageCache | RenderingFlags.Annotations | RenderingFlags.DontCatch |
                       RenderingFlags.LcdText);

            bitmap.Freeze();
            return bitmap;
        }
    }
}