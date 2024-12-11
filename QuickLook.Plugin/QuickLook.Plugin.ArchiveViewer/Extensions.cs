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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace QuickLook.Plugin.ArchiveViewer;

public static class Extensions
{
    public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
    {
        foreach (var item in enumeration)
            action(item);
    }

    public static T GetDescendantByType<T>(this Visual element) where T : class
    {
        if (element == null)
            return default(T);
        if (element.GetType() == typeof(T))
            return element as T;

        T foundElement = null;
        (element as FrameworkElement)?.ApplyTemplate();

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            var visual = VisualTreeHelper.GetChild(element, i) as Visual;
            foundElement = visual.GetDescendantByType<T>();
            if (foundElement != null)
                break;
        }

        return foundElement;
    }
}
