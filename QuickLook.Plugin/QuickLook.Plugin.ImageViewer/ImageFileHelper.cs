using System;
using System.Windows;
using ExifLib;
using ImageMagick;

namespace QuickLook.Plugin.ImageViewer
{
    internal static class ImageFileHelper
    {
        internal static Size? GetImageSize(string path)
        {
            var ori = GetOrientationFromExif(path);

            try
            {
                var info = new MagickImageInfo(path);

                if (ori == OrientationType.RightTop || ori == OrientationType.LeftBotom)
                    return new Size {Width = info.Height, Height = info.Width};
                return new Size {Width = info.Width, Height = info.Height};
            }
            catch (MagickException)
            {
                return null;
            }
        }

        private static OrientationType GetOrientationFromExif(string path)
        {
            try
            {
                using (var re = new ExifReader(path))
                {
                    re.GetTagValue(ExifTags.Orientation, out ushort orientation);

                    if (orientation == 0)
                        return OrientationType.Undefined;

                    return (OrientationType) orientation;
                }
            }
            catch (Exception)
            {
                return OrientationType.Undefined;
            }
        }
    }
}