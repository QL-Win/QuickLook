using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickLook.Converters
{
    public sealed class BooleanToVisibilityCollapsedConverter : DependencyObject, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Visible;

            var v = (bool) value;

            return v ? Visibility.Visible : Visibility.Collapsed;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}