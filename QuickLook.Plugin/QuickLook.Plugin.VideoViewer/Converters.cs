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

namespace QuickLook.Plugin.VideoViewer
{
    public sealed class TimeSpanToSecondsConverter : DependencyObject, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan span)
                return span.TotalSeconds;
            if (value is Duration duration)
                return duration.HasTimeSpan ? duration.TimeSpan.TotalSeconds : 0d;

            return 0d;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var span = TimeSpan.Zero;

            if (value != null)
                span = TimeSpan.FromSeconds((double) value);

            if (targetType == typeof(TimeSpan))
                return span;
            if (targetType == typeof(Duration))
                return new Duration(span);

            return Activator.CreateInstance(targetType);
        }
    }

    public sealed class TimeSpanToShortStringConverter : DependencyObject, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "00:00";

            var span = TimeSpan.Zero;
            if (value is Duration duration)
                span = duration.HasTimeSpan ? duration.TimeSpan : TimeSpan.Zero;
            if (value is TimeSpan timespan)
                span = timespan;

            var s = string.Empty;
            if (span.Hours > 0)
                s += $"{span.Hours:D2}:";

            s += $"{span.Minutes:D2}:{span.Seconds:D2}";

            return s;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class VolumeToIconConverter : DependencyObject, IValueConverter
    {
        private static readonly string[] Volumes = {"\xE992", "\xE993", "\xE994", "\xE995"};

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Volumes[0];

            var v = (int) Math.Min(100, Math.Max((double) value * 100, 0));

            return v == 0 ? Volumes[0] : Volumes[1 + v / 34];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}