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
using System.Windows;
using System.Windows.Data;

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
    private static readonly string[] Volumes = { "\xE74F", "\xE993", "\xE994", "\xE995" };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return Volumes[0];

        var v = (double)value;
        if (Math.Abs(v) < 0.01)
            return Volumes[0];

        v = Math.Min(v, 1);

        return Volumes[1 + (int)(v / 0.34)];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
