using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickLook.Plugin.ArchiveViewer
{
    public class LevelToIndentConverter : DependencyObject, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var level = (int) values[0];
            var indent = (double) values[1];
            return indent * level;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LevelToBooleanConverter : DependencyObject, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var level = (int) value;

            return level < 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToAsteriskConverter : DependencyObject, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var b = (bool) value;

            return b ? "*" : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SizePrettyPrintConverter : DependencyObject, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var size = (ulong) values[0];
            var isFolder = (bool) values[1];

            return isFolder ? "" : size.ToPrettySize(2);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DatePrintConverter : DependencyObject, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var date = (DateTime) values[0];
            var isFolder = (bool) values[1];

            return isFolder ? "" : date.ToString(CultureInfo.CurrentCulture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FileExtToIconConverter : DependencyObject, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var name = (string) values[0];
            var isFolder = (bool) values[1];

            if (isFolder)
                return IconManager.FindIconForDir(false);

            return IconManager.FindIconForFilename(name, false);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}