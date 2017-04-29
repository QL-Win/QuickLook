using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace QuickLook.Plugin.ArchiveViewer
{
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

        public static string ToPrettySize(this ulong value, int decimalPlaces = 0)
        {
            const long OneKb = 1024;
            const long OneMb = OneKb * 1024;
            const long OneGb = OneMb * 1024;
            const long OneTb = OneGb * 1024;

            var asTb = Math.Round((double) value / OneTb, decimalPlaces);
            var asGb = Math.Round((double) value / OneGb, decimalPlaces);
            var asMb = Math.Round((double) value / OneMb, decimalPlaces);
            var asKb = Math.Round((double) value / OneKb, decimalPlaces);
            var chosenValue = asTb > 1
                ? $"{asTb} TB"
                : asGb > 1
                    ? $"{asGb} GB"
                    : asMb > 1
                        ? $"{asMb} MB"
                        : asKb > 1
                            ? $"{asKb} KB"
                            : $"{Math.Round((double) value, decimalPlaces)} bytes";

            return chosenValue;
        }
    }
}