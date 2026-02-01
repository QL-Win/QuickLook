// Copyright © 2017-2026 QL-Win Contributors
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

using QuickLook.Common.Controls;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.VideoViewer;

public sealed class TimeTickToShortStringConverter : DependencyObject, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return "00:00";

        var v = TimeSpan.FromTicks((long)value);

        var s = string.Empty;
        if (v.Hours > 0)
            s += $"{v.Hours:D2}:";

        s += $"{v.Minutes:D2}:{v.Seconds:D2}";

        return s;
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class VolumeToIconConverter : DependencyObject, IValueConverter
{
    private static readonly string[] Volumes = [FontSymbols.Mute, FontSymbols.Volume1, FontSymbols.Volume2, FontSymbols.Volume3];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double v)
        {
            // Clump to range [0, 1]
            v = Math.Max(0d, Math.Min(v, 1d));

            if (v < 0.01d) return Volumes[0];

            return Volumes[1 + (int)(v / 0.34d)];
        }

        return Volumes[0];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class CoverArtConverter : IValueConverter
{
    private static readonly BitmapImage emptyImage =
        new(new Uri("pack://application:,,,/QuickLook.Plugin.VideoViewer;component/Resources/empty.png", UriKind.Absolute));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value ?? emptyImage;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class TimeToLongConverter : DependencyObject, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long v)
            return TimeSpan.FromTicks((long)v);
        else
            return TimeSpan.FromTicks(0L);
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TimeSpan v)
            return ((TimeSpan)v).Ticks;
        else
            return 0L;

    }
}

public sealed class TimeToShortStringConverter : DependencyObject, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return "00:00";

        var v = (TimeSpan)value;

        var s = string.Empty;
        if (v.Hours > 0)
            s += $"{v.Hours:D2}:";

        s += $"{v.Minutes:D2}:{v.Seconds:D2}";

        return s;
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
