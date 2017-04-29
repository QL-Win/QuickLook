using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.InfoPanel
{
    public static class Extensions
    {
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        public static BitmapSource ToBitmapSource(this Bitmap source)
        {
            // BitmapSource.Create throws an exception when the image is scanned backward.
            // The Clone() will make it back scanning forward.
            source = (Bitmap) source.Clone();

            var ip = source.GetHbitmap();
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
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }
    }
}