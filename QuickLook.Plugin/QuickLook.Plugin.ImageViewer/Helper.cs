// Copyright © 2017-2025 QL-Win Contributors
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

using QuickLook.Common.Helpers;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ImageViewer;

internal static class Helper
{
    public static BitmapSource InvertColors(this BitmapSource source)
    {
        WriteableBitmap writableBitmap = new(source);
        writableBitmap.Lock();

        nint pBackBuffer = writableBitmap.BackBuffer;
        int stride = writableBitmap.BackBufferStride;
        int width = writableBitmap.PixelWidth;
        int height = writableBitmap.PixelHeight;
        int bytesPerPixel = (writableBitmap.Format.BitsPerPixel + 7) / 8;

        unsafe
        {
            byte* pPixels = (byte*)pBackBuffer;

            for (int y = 0; y < height; y++)
            {
                Span<byte> row = new(pPixels + y * stride, width * bytesPerPixel);

                for (int x = 0; x < width; x++)
                {
                    int index = x * bytesPerPixel;

                    row[index] = (byte)(255 - row[index]);
                    row[index + 1] = (byte)(255 - row[index + 1]);
                    row[index + 2] = (byte)(255 - row[index + 2]);
                }
            }
        }

        writableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        writableBitmap.Unlock();

        return writableBitmap;
    }

    public static void DpiHack(this BitmapSource img)
    {
        // a dirty hack... but is the fastest

        var newDpiX = (double)DisplayDeviceHelper.DefaultDpi * DisplayDeviceHelper.GetCurrentScaleFactor().Horizontal;
        var newDpiY = (double)DisplayDeviceHelper.DefaultDpi * DisplayDeviceHelper.GetCurrentScaleFactor().Vertical;

        var dpiX = img.GetType().GetField("_dpiX",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var dpiY = img.GetType().GetField("_dpiY",
            BindingFlags.NonPublic | BindingFlags.Instance);
        dpiX?.SetValue(img, newDpiX);
        dpiY?.SetValue(img, newDpiY);
    }

    public static Uri FilePathToFileUrl(string filePath)
    {
        var uri = new StringBuilder();
        foreach (var v in filePath)
            if (v >= 'a' && v <= 'z' || v >= 'A' && v <= 'Z' || v >= '0' && v <= '9' ||
                v == '+' || v == '/' || v == ':' || v == '.' || v == '-' || v == '_' || v == '~' ||
                v > '\x80')
                uri.Append(v);
            else if (v == Path.DirectorySeparatorChar || v == Path.AltDirectorySeparatorChar)
                uri.Append('/');
            else
                uri.Append($"%{(int)v:X2}");
        if (uri.Length >= 2 && uri[0] == '/' && uri[1] == '/') // UNC path
            uri.Insert(0, "file:");
        else
            uri.Insert(0, "file:///");

        try
        {
            return new Uri(uri.ToString());
        }
        catch
        {
            return null;
        }
    }
}
