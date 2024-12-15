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
using System.Runtime.InteropServices;

namespace QuickLook.Plugin.PEViewer;

[Flags]
internal enum ThumbnailOptions
{
    None = 0x00,
    BiggerSizeOk = 0x01,
    InMemoryOnly = 0x02,
    IconOnly = 0x04,
    ThumbnailOnly = 0x08,
    InCacheOnly = 0x10,
    IconBackground = 0x80,
    ScaleUp = 0x100
}

internal static class WindowsThumbnailProvider
{
    private const string IShellItem2Guid = "7E9FB0D3-919F-4307-AB2E-9B1860310C93";

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string path,
        // The following parameter is not used - binding context.
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);

    public static Bitmap GetThumbnail(string fileName, int width, int height, ThumbnailOptions options)
    {
        var hBitmap = GetHBitmap(Path.GetFullPath(fileName), width, height, options);

        if (hBitmap == IntPtr.Zero)
            return null!;

        try
        {
            // return a System.Drawing.Bitmap from the hBitmap
            return GetBitmapFromHBitmap(hBitmap);
        }
        finally
        {
            // delete HBitmap to avoid memory leaks
            DeleteObject(hBitmap);
        }
    }

    internal static Bitmap GetBitmapFromHBitmap(IntPtr nativeHBitmap)
    {
        var bmp = Image.FromHbitmap(nativeHBitmap);

        if (Image.GetPixelFormatSize(bmp.PixelFormat) < 32)
            return bmp;

        return CreateAlphaBitmap(bmp, PixelFormat.Format32bppArgb);
    }

    internal static Bitmap CreateAlphaBitmap(Bitmap srcBitmap, PixelFormat targetPixelFormat)
    {
        var result = new Bitmap(srcBitmap.Width, srcBitmap.Height, targetPixelFormat);

        var bmpBounds = new Rectangle(0, 0, srcBitmap.Width, srcBitmap.Height);

        var srcData = srcBitmap.LockBits(bmpBounds, ImageLockMode.ReadOnly, srcBitmap.PixelFormat);

        var isAlplaBitmap = false;

        try
        {
            for (var y = 0; y <= srcData.Height - 1; y++)
                for (var x = 0; x <= srcData.Width - 1; x++)
                {
                    var pixelColor = Color.FromArgb(
                        Marshal.ReadInt32(srcData.Scan0, srcData.Stride * y + 4 * x));

                    if ((pixelColor.A > 0) & (pixelColor.A < 255))
                        isAlplaBitmap = true;

                    result.SetPixel(x, y, pixelColor);
                }
        }
        finally
        {
            srcBitmap.UnlockBits(srcData);
        }

        if (isAlplaBitmap)
            return result;
        return srcBitmap;
    }

    private static IntPtr GetHBitmap(string fileName, int width, int height, ThumbnailOptions options)
    {
        var shellItem2Guid = new Guid(IShellItem2Guid);
        var retCode =
            SHCreateItemFromParsingName(fileName, IntPtr.Zero, ref shellItem2Guid, out var nativeShellItem);

        if (retCode != 0)
            return IntPtr.Zero;

        var nativeSize = new NativeSize
        {
            Width = width,
            Height = height
        };

        var hr = ((IShellItemImageFactory)nativeShellItem).GetImage(nativeSize, options, out var hBitmap);

        Marshal.ReleaseComObject(nativeShellItem);

        return hr == HResult.Ok ? hBitmap : IntPtr.Zero;
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    internal interface IShellItem
    {
        void BindToHandler(IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid bhid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            out IntPtr ppv);

        void GetParent(out IShellItem ppsi);

        void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);

        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    internal enum SIGDN : uint
    {
        NORMALDISPLAY = 0,
        PARENTRELATIVEPARSING = 0x80018001,
        PARENTRELATIVEFORADDRESSBAR = 0x8001c001,
        DESKTOPABSOLUTEPARSING = 0x80028000,
        PARENTRELATIVEEDITING = 0x80031001,
        DESKTOPABSOLUTEEDITING = 0x8004c000,
        FILESYSPATH = 0x80058000,
        URL = 0x80068000
    }

    internal enum HResult
    {
        Ok = 0x0000,
        False = 0x0001,
        InvalidArguments = unchecked((int)0x80070057),
        OutOfMemory = unchecked((int)0x8007000E),
        NoInterface = unchecked((int)0x80004002),
        Fail = unchecked((int)0x80004005),
        ElementNotFound = unchecked((int)0x80070490),
        TypeElementNotFound = unchecked((int)0x8002802B),
        NoObject = unchecked((int)0x800401E5),
        Win32ErrorCanceled = 1223,
        Canceled = unchecked((int)0x800704C7),
        ResourceInUse = unchecked((int)0x800700AA),
        AccessDenied = unchecked((int)0x80030005)
    }

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellItemImageFactory
    {
        [PreserveSig]
        HResult GetImage(
            [In][MarshalAs(UnmanagedType.Struct)] NativeSize size,
            [In] ThumbnailOptions flags,
            [Out] out IntPtr phbm);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeSize
    {
        private int width;
        private int height;

        public int Width
        {
            set => width = value;
        }

        public int Height
        {
            set => height = value;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RGBQUAD
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
        public byte rgbReserved;
    }
}
