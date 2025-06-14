using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Dmg;

public static class IcnsParser
{
    public static Bitmap Parse(byte[] icnsBytes)
    {
        // Temporary method

        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
            .Where((assembly) => assembly.FullName.StartsWith("QuickLook.Plugin.ImageViewer"))
            .FirstOrDefault();

        if (assembly == null)
            return null;

        Type type = assembly.GetTypes()
            .Where(type => type.Name.StartsWith("IcnsImageParser"))
            .FirstOrDefault();

        if (type == null)
            return null;

        MethodInfo method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == "GetImages"
                && method.GetParameters().FirstOrDefault()?.ParameterType == typeof(Stream))
            .FirstOrDefault();

        if (method == null)
            return null;

        using MemoryStream stream = new(icnsBytes);
        dynamic[] images = method.Invoke(null, [stream]) as dynamic[];
        List<Bitmap> bitmaps = [];

        foreach (dynamic image in images)
        {
            if (image.GetType().GetProperty("Bitmap") is PropertyInfo property)
            {
                var bitmap = property.GetValue(image);
                bitmaps.Add(bitmap);
            }
        }

        Bitmap imageResult = bitmaps
            .Where(bitmap => bitmap != null)
            .OrderByDescending(bitmap => bitmap.Width)
            .FirstOrDefault()
            ?.Clone() as Bitmap;

        foreach (dynamic image in images)
        {
            if (image.GetType().GetProperty("Bitmap") is PropertyInfo property)
            {
                if (property.GetValue(image) is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        return imageResult;
    }
}
