using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.PDFViewer
{
    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (var item in enumeration)
                action(item);
        }

        public static T GetDescendantByType<T>(this Visual element) where T : class
        {
            if (element == null)
                return default(T);
            if (element.GetType() == typeof(T))
                return element as T;

            T foundElement = null;
            (element as FrameworkElement)?.ApplyTemplate();

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var visual = VisualTreeHelper.GetChild(element, i) as Visual;
                foundElement = visual.GetDescendantByType<T>();
                if (foundElement != null)
                    break;
            }
            return foundElement;
        }

        public static BitmapSource ToBitmapSource(this Bitmap source)
        {
            BitmapSource bs = null;
            try
            {
                var data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height),
                    ImageLockMode.ReadOnly, source.PixelFormat);

                bs = BitmapSource.Create(source.Width, source.Height, source.HorizontalResolution,
                    source.VerticalResolution, PixelFormats.Bgr24, null,
                    data.Scan0, data.Stride * source.Height, data.Stride);

                source.UnlockBits(data);

                bs.Freeze();
            }
            catch
            {
                // ignored
            }

            return bs;
        }
    }
}