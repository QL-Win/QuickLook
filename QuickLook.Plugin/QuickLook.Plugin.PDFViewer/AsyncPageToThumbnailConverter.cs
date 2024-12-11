// Copyright © 2018 Paddy Xu
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
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.PDFViewer;

internal class AsyncPageToThumbnailConverter : IMultiValueConverter
{
    private static readonly BitmapImage Loading =
        new BitmapImage(
            new Uri("pack://application:,,,/QuickLook.Plugin.PdfViewer;component/Resources/loading.png"));

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            throw new Exception("PageIdToImageConverter");

        if (values[0] is not PdfDocumentWrapper handle) return null;

        var pageId = (int)values[1];
        if (pageId < 0) return null;

        var task = Task.Run(() =>
        {
            try
            {
                return handle.RenderThumbnail(pageId);
            }
            catch (Exception)
            {
                return Loading;
            }
        });

        return new NotifyTaskCompletion<BitmapSource>(task, Loading);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
