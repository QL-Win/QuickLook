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