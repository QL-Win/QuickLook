using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ImageViewer
{
    internal static class ImageFileHelper
    {
        internal static Size GetImageSize(string path)
        {
            var ori = GetOrientationFromExif(path);

            using (var stream = File.OpenRead(path))
            {
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
                var frame = decoder.Frames[0];

                if (ori == ExifOrientation.Rotate90CW || ori == ExifOrientation.Rotate270CW)
                    return new Size {Width = frame.PixelHeight, Height = frame.PixelWidth};

                return new Size {Width = frame.PixelWidth, Height = frame.PixelHeight};
            }
        }

        internal static ExifOrientation GetOrientationFromExif(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.None);
                var frame = decoder.Frames[0];

                var orientation = ((BitmapMetadata) frame.Metadata)?.GetQuery(@"/app1/{ushort=0}/{ushort=274}");

                if (orientation == null)
                    return ExifOrientation.Horizontal;

                return (ExifOrientation) (ushort) orientation;
            }
        }

        internal enum ExifOrientation
        {
            Horizontal = 1,
            MirrorHorizontal = 2,
            Rotate180 = 3,
            MirrorVertical = 4,
            MirrorHorizontal270CW = 5,
            Rotate90CW = 6,
            MirrorHorizontal90CW = 7,
            Rotate270CW = 8
        }
    }
}