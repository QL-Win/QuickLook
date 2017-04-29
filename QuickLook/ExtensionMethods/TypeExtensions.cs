using System;

namespace QuickLook.ExtensionMethods
{
    internal static class TypeExtensions
    {
        public static T CreateInstance<T>(this Type t)
        {
            return (T) Activator.CreateInstance(t);
        }
    }
}