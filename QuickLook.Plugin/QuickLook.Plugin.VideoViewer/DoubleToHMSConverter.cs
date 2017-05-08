using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickLook.Plugin.VideoViewer
{
    public sealed class DecimalToTimeSpanConverter : DependencyObject, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "00:00:00";

            var time = TimeSpan.FromSeconds((double) (decimal) value);

            return time.ToString(@"hh\:mm\:ss");
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class DoubleToTimeSpanConverter : DependencyObject, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "00:00:00";

            var time = TimeSpan.FromSeconds((double) value);

            return time.ToString(@"hh\:mm\:ss");
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}