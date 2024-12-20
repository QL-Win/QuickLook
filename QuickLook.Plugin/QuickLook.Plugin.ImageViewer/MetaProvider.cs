// Copyright © 2020 Paddy Xu
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

using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Xml;

namespace QuickLook.Plugin.ImageViewer;

public class MetaProvider
{
    private readonly SortedDictionary<string, (string, string)> _cache = []; // [key, [label, value]]

    private readonly string _path;

    public MetaProvider(string path)
    {
        _path = path;

        GetExif();
    }

    public SortedDictionary<string, (string, string)> GetExif()
    {
        if (_cache.Count != 0)
            return _cache;

        var exif = NativeMethods.GetExif(_path);
        if (string.IsNullOrEmpty(exif))
            return _cache;

        var xml = new XmlDocument();
        xml.LoadXml(exif);
        var iter = xml.SelectNodes("/Exif/child::node()")?.GetEnumerator();
        while (iter != null && iter.MoveNext())
        {
            if (iter.Current is not XmlNode node)
                continue;

            var key = node.Name;
            var label = node.Attributes?["Label"]?.InnerText;
            var value = node.InnerText;

            _cache.Add(key, (label, value));
        }

        return _cache;
    }

    public byte[] GetThumbnail()
    {
        return NativeMethods.GetThumbnail(_path) ?? [];
    }

    public Size GetSize()
    {
        _cache.TryGetValue("_.Size.Width", out var w_);
        _cache.TryGetValue("_.Size.Height", out var h_);

        if (int.TryParse(w_.Item2, out var w) && int.TryParse(h_.Item2, out var h))
            return new Size(w, h);

        // fallback

        try
        {
            using (var mi = new MagickImage())
            {
                mi.Ping(_path);
                w = (int)mi.Width;
                h = (int)mi.Height;
            }
        }
        catch
        {
            // There are always formats that MagickImage does not support
            // TODO: Use MediaInfo to detect it?
            return Size.Empty;
        }

        return w + h == 0 ? new Size(800, 600) : new Size(w, h);
    }

    public Orientation GetOrientation()
    {
        return (Orientation)NativeMethods.GetOrientation(_path);
    }
}

internal static class NativeMethods
{
    private static readonly bool Is64 = Environment.Is64BitProcess;

    public static string GetExif(string file)
    {
        try
        {
            var len = Is64 ? GetExif_64(file, null) : GetExif_32(file, null);
            if (len <= 0)
                return string.Empty;

            var sb = new StringBuilder(len + 1);
            var _ = Is64 ? GetExif_64(file, sb) : GetExif_32(file, sb);

            return sb.ToString();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return string.Empty;
        }
    }

    public static byte[] GetThumbnail(string file)
    {
        try
        {
            var len = Is64 ? GetThumbnail_64(file, null) : GetThumbnail_32(file, null);
            if (len <= 0)
                return null;

            var buffer = new byte[len];
            var _ = Is64 ? GetThumbnail_64(file, buffer) : GetThumbnail_32(file, buffer);

            return buffer;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return null;
        }
    }

    public static int GetOrientation(string file)
    {
        try
        {
            return Is64 ? GetOrientation_64(file) : GetOrientation_32(file);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return 0;
        }
    }

    [DllImport("exiv2-ql-32.dll", EntryPoint = "GetExif", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetExif_32([MarshalAs(UnmanagedType.LPWStr)] string file,
        [MarshalAs(UnmanagedType.LPStr)] StringBuilder sb);

    [DllImport("exiv2-ql-32.dll", EntryPoint = "GetThumbnail", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetThumbnail_32([MarshalAs(UnmanagedType.LPWStr)] string file,
        [MarshalAs(UnmanagedType.LPArray)] byte[] buffer);

    [DllImport("exiv2-ql-32.dll", EntryPoint = "GetOrientation", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetOrientation_32([MarshalAs(UnmanagedType.LPWStr)] string file);

    [DllImport("exiv2-ql-64.dll", EntryPoint = "GetExif", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetExif_64([MarshalAs(UnmanagedType.LPWStr)] string file,
        [MarshalAs(UnmanagedType.LPStr)] StringBuilder sb);

    [DllImport("exiv2-ql-64.dll", EntryPoint = "GetThumbnail", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetThumbnail_64([MarshalAs(UnmanagedType.LPWStr)] string file,
        [MarshalAs(UnmanagedType.LPArray)] byte[] buffer);

    [DllImport("exiv2-ql-64.dll", EntryPoint = "GetOrientation", CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetOrientation_64([MarshalAs(UnmanagedType.LPWStr)] string file);
}

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
    LeftBottom = 8
}
