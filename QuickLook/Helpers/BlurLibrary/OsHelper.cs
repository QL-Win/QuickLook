using System;

namespace QuickLook.Helpers.BlurLibrary
{
    internal static class OsHelper
    {
        public static OsType GetOsType()
        {
            if (Environment.OSVersion.Version.Major != 6 && Environment.OSVersion.Version.Major != 10)
                return OsType.Other;

            if (Environment.OSVersion.Version.Major != 6)
                return Environment.OSVersion.Version.Major == 10
                    ? OsType.Windows10
                    : OsType.Other;

            switch (Environment.OSVersion.Version.Minor)
            {
                case 0:
                    return OsType.WindowsVista;
                case 1:
                    return OsType.Windows7;
                case 2:
                    return OsType.Windows8;
                case 3:
                    return OsType.Windows81;
                default:
                    return OsType.Other;
            }
        }
    }
}