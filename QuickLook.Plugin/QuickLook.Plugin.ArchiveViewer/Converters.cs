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

using QuickLook.Common.ExtensionMethods;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickLook.Plugin.ArchiveViewer;

public class Percent100ToVisibilityVisibleConverter : DependencyObject, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            value = 0;

        var percent = (double)value;
        return Math.Abs(percent - 100) < 0.00001 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class Percent100ToVisibilityCollapsedConverter : DependencyObject, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            value = 0;

        var percent = (double)value;
        return Math.Abs(percent - 100) < 0.00001 ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class LevelToIndentConverter : DependencyObject, IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] == DependencyProperty.UnsetValue)
            values[0] = 1;

        var level = (int)values[0];
        var indent = (double)values[1];
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
        var level = (int)value;

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
        var b = (bool)value;

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
        var size = (ulong)values[0];
        var isFolder = (bool)values[1];

        return isFolder ? "" : ((long)size).ToPrettySize(2);
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
        var date = (DateTime)values[0];
        var isFolder = (bool)values[1];

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
        var name = (string)values[0];
        var isFolder = (bool)values[1];

        if (isFolder)
            return IconManager.FindIconForDir(false);

        return IconManager.FindIconForFilename(name, false);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
