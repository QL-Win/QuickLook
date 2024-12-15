// Copyright © 2024 Frank Becker
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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace QuickLook.Plugin.HtmlViewer.NativeMethods;

internal static class ShlwApi
{
    public static string GetAssociatedAppForScheme(string scheme)
    {
        try
        {
            // Try to get friendly app name first
            uint pcchOut = 0;
            AssocQueryString(AssocF.None, AssocStr.FriendlyAppName, scheme, null, null, ref pcchOut);

            if (pcchOut > 0)
            {
                var pszOut = new StringBuilder((int)pcchOut);
                AssocQueryString(AssocF.None, AssocStr.FriendlyAppName, scheme, null, pszOut, ref pcchOut);

                var appName = pszOut.ToString().Trim();
                if (!string.IsNullOrEmpty(appName))
                    return appName;
            }

            // Fall back to executable name if friendly name is not available
            pcchOut = 0;
            AssocQueryString(AssocF.None, AssocStr.Executable, scheme, null, null, ref pcchOut);

            if (pcchOut > 0)
            {
                var pszOut = new StringBuilder((int)pcchOut);
                AssocQueryString(AssocF.None, AssocStr.Executable, scheme, null, pszOut, ref pcchOut);

                var exeName = pszOut.ToString().Trim();
                if (!string.IsNullOrEmpty(exeName))
                    return Path.GetFileName(exeName);
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to get associated app: {ex.Message}");
            return null;
        }
    }

    [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
    public static extern uint AssocQueryString(
        AssocF flags,
        AssocStr str,
        string pszAssoc,
        string pszExtra,
        [Out] StringBuilder pszOut,
        ref uint pcchOut
    );

    [Flags]
    public enum AssocF
    {
        None = 0,
        VerifyExists = 0x1
    }

    public enum AssocStr
    {
        Command = 1,
        Executable = 2,
        FriendlyAppName = 4
    }
}
