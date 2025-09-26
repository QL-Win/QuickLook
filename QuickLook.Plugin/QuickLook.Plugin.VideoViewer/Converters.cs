﻿// Copyright © 2017-2025 QL-Win Contributors
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

        try
        {
            var v = TimeSpan.FromTicks((long)value);

            var s = string.Empty;
            if (v.Hours > 0)
                s += $"{v.Hours:D2}:";

            s += $"{v.Minutes:D2}:{v.Seconds:D2}";

            return s;
        }
        catch
        {
            return "00:00";
        }
    }

    object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class VolumeToIconConverter : DependencyObject, IValueConverter
{
    private static readonly string[] Volumes = ["\xE74F", "\xE993", "\xE994", "\xE995"];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double v && !double.IsNaN(v) && !double.IsInfinity(v))
        {
            // Clamp to range [0, 1]
            v = Math.Max(0d, Math.Min(v, 1d));

            if (v < 0.01d) return Volumes[0];

            // Calculate index and ensure it's within bounds to prevent IndexOutOfRangeException
            int index = Math.Min(1 + (int)(v / 0.34d), Volumes.Length - 1);
            return Volumes[index];
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
