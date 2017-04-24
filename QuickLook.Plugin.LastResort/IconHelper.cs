using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace QuickLook.Plugin.LastResort
{
    public class IconHelper
    {
        public enum IconSizeEnum
        {
            SmallIcon16 = SHGFI_SMALLICON,
            MediumIcon32 = SHGFI_LARGEICON,
            LargeIcon48 = SHIL_EXTRALARGE,
            ExtraLargeIcon = SHIL_JUMBO
        }

        private const int SHGFI_SMALLICON = 0x1;
        private const int SHGFI_LARGEICON = 0x0;
        private const int SHIL_JUMBO = 0x4;
        private const int SHIL_EXTRALARGE = 0x2;
        private const int WM_CLOSE = 0x0010;

        [DllImport("user32")]
        private static extern
            IntPtr SendMessage(
                IntPtr handle,
                int Msg,
                IntPtr wParam,
                IntPtr lParam);


        [DllImport("shell32.dll")]
        private static extern int SHGetImageList(
            int iImageList,
            ref Guid riid,
            out IImageList ppv);

        [DllImport("Shell32.dll")]
        private static extern int SHGetFileInfo(
            string pszPath,
            int dwFileAttributes,
            ref SHFILEINFO psfi,
            int cbFileInfo,
            uint uFlags);

        [DllImport("user32")]
        private static extern int DestroyIcon(
            IntPtr hIcon);

        public static Bitmap GetBitmapFromFolderPath(
            string filepath, IconSizeEnum iconsize)
        {
            var hIcon = GetIconHandleFromFolderPath(filepath, iconsize);
            return getBitmapFromIconHandle(hIcon);
        }

        public static Bitmap GetBitmapFromFilePath(
            string filepath, IconSizeEnum iconsize)
        {
            var hIcon = GetIconHandleFromFilePath(filepath, iconsize);
            return getBitmapFromIconHandle(hIcon);
        }

        public static Bitmap GetBitmapFromPath(
            string filepath, IconSizeEnum iconsize)
        {
            var hIcon = IntPtr.Zero;
            if (Directory.Exists(filepath))
            {
                hIcon = GetIconHandleFromFolderPath(filepath, iconsize);
            }
            else
            {
                if (File.Exists(filepath))
                    hIcon = GetIconHandleFromFilePath(filepath, iconsize);
            }
            return getBitmapFromIconHandle(hIcon);
        }

        private static Bitmap getBitmapFromIconHandle(IntPtr hIcon)
        {
            if (hIcon == IntPtr.Zero) throw new FileNotFoundException();
            var myIcon = Icon.FromHandle(hIcon);
            var bitmap = myIcon.ToBitmap();
            myIcon.Dispose();
            DestroyIcon(hIcon);
            SendMessage(hIcon, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            return bitmap;
        }

        private static IntPtr GetIconHandleFromFilePath(string filepath, IconSizeEnum iconsize)
        {
            var shinfo = new SHFILEINFO();
            const uint SHGFI_SYSICONINDEX = 0x4000;
            const int FILE_ATTRIBUTE_NORMAL = 0x80;
            var flags = SHGFI_SYSICONINDEX;
            return getIconHandleFromFilePathWithFlags(filepath, iconsize, ref shinfo, FILE_ATTRIBUTE_NORMAL, flags);
        }

        private static IntPtr GetIconHandleFromFolderPath(string folderpath, IconSizeEnum iconsize)
        {
            var shinfo = new SHFILEINFO();

            const uint SHGFI_ICON = 0x000000100;
            const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
            const int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
            var flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;
            return getIconHandleFromFilePathWithFlags(folderpath, iconsize, ref shinfo, FILE_ATTRIBUTE_DIRECTORY,
                flags);
        }

        private static IntPtr getIconHandleFromFilePathWithFlags(
            string filepath, IconSizeEnum iconsize,
            ref SHFILEINFO shinfo, int fileAttributeFlag, uint flags)
        {
            const int ILD_TRANSPARENT = 1;
            var retval = SHGetFileInfo(filepath, fileAttributeFlag, ref shinfo, Marshal.SizeOf(shinfo), flags);
            if (retval == 0) throw new FileNotFoundException();
            var iconIndex = shinfo.iIcon;
            var iImageListGuid = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
            IImageList iml;
            var hres = SHGetImageList((int) iconsize, ref iImageListGuid, out iml);
            var hIcon = IntPtr.Zero;
            hres = iml.GetIcon(iconIndex, ILD_TRANSPARENT, ref hIcon);
            return hIcon;
        }
    }

    [ComImport]
    [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IImageList
    {
        [PreserveSig]
        int Add(
            IntPtr hbmImage,
            IntPtr hbmMask,
            ref int pi);

        [PreserveSig]
        int ReplaceIcon(
            int i,
            IntPtr hicon,
            ref int pi);

        [PreserveSig]
        int SetOverlayImage(
            int iImage,
            int iOverlay);

        [PreserveSig]
        int Replace(
            int i,
            IntPtr hbmImage,
            IntPtr hbmMask);

        [PreserveSig]
        int AddMasked(
            IntPtr hbmImage,
            int crMask,
            ref int pi);

        [PreserveSig]
        int Draw(
            ref IMAGELISTDRAWPARAMS pimldp);

        [PreserveSig]
        int Remove(
            int i);

        [PreserveSig]
        int GetIcon(
            int i,
            int flags,
            ref IntPtr picon);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGEINFO
    {
        public IntPtr hbmImage;
        public IntPtr hbmMask;
        public int Unused1;
        public int Unused2;
        public RECT rcImage;
    }

    public struct IMAGELISTDRAWPARAMS
    {
        public int cbSize;
        public IntPtr himl;
        public int i;
        public IntPtr hdcDst;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int xBitmap;
        public int yBitmap;
        public int rgbBk;
        public int rgbFg;
        public int fStyle;
        public int dwRop;
        public int fState;
        public int Frame;
        public int crEffect;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        private readonly int X;
        private readonly int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }

        public POINT(Point pt) : this(pt.X, pt.Y)
        {
        }

        public static implicit operator Point(POINT p)
        {
            return new Point(p.X, p.Y);
        }

        public static implicit operator POINT(Point p)
        {
            return new POINT(p.X, p.Y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 254)] public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szTypeName;
    }
}