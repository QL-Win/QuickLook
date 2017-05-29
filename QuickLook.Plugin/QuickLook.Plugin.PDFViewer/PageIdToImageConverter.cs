using System;
using System.Globalization;
using System.Windows.Data;

namespace QuickLook.Plugin.PDFViewer
{
    public sealed class PageIdToImageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                throw new Exception("PageIdToImageConverter");

            var zoom = 0.3f;
            if (parameter != null)
                float.TryParse((string) parameter, out zoom);

            var handle = values[0] as PdfFile;
            if (handle == null) return null;

            var pageId = (int) values[1];
            if (pageId < 0) return null;

            var bitmap = handle.GetPage(pageId, zoom);
            var bs = bitmap.ToBitmapSource();
            bitmap.Dispose();

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);

            return bs;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}