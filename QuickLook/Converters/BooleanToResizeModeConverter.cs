using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickLook.Converters
{
    public sealed class BooleanToResizeModeConverter : DependencyObject, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return ResizeMode.CanResize;

            var v = (bool) value;

            return v ? ResizeMode.CanResize : ResizeMode.NoResize;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}