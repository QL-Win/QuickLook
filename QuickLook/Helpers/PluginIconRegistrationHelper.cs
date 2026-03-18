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

using Microsoft.Win32;
using QuickLook.Common.Helpers;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace QuickLook.Helpers;

internal static class PluginIconRegistrationHelper
{
    internal static void CheckAndRegisterPluginIcon()
    {
        try
        {
            var isElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);
            if (!isElevated)
                return;

            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QLPlugin.ico");
            if (!File.Exists(iconPath))
                return;

            bool changed = false;

            // Computer\HKEY_CLASSES_ROOT\.qlplugin
            using (var key = Registry.ClassesRoot.CreateSubKey(".qlplugin", true))
            {
                if (key != null)
                {
                    if (key.GetValue(string.Empty) as string != "QuickLook.Plugin")
                    {
                        key.SetValue(string.Empty, "QuickLook.Plugin");
                        changed = true;
                    }

                    if (key.GetValue("PerceivedType") as string != "compressed")
                    {
                        key.SetValue("PerceivedType", "compressed");
                        changed = true;
                    }
                }
            }

            // Computer\HKEY_CLASSES_ROOT\QuickLook.Plugin\DefaultIcon
            using (var key = Registry.ClassesRoot.CreateSubKey(@"QuickLook.Plugin\DefaultIcon", true))
            {
                var iconValue = $"{iconPath},0";
                if (key != null && key.GetValue("") as string != iconValue)
                {
                    key.SetValue("", iconValue);
                    changed = true;
                }
            }

            // Computer\HKEY_CLASSES_ROOT\QuickLook.Plugin
            using (var key = Registry.ClassesRoot.CreateSubKey("QuickLook.Plugin", true))
            {
                const string fileTypeName = "QuickLook Plugin File";
                if (key != null && key.GetValue(string.Empty) as string != fileTypeName)
                {
                    key.SetValue(string.Empty, fileTypeName);
                    changed = true;
                }
            }

            // Computer\HKEY_CLASSES_ROOT\QuickLook.Plugin\shell\open\command
            using (var key = Registry.ClassesRoot.CreateSubKey(@"QuickLook.Plugin\shell\open\command", true))
            {
                var commandValue = $"\"{App.AppFullPath}\" \"%1\"";
                if (key != null && key.GetValue(string.Empty) as string != commandValue)
                {
                    key.SetValue(string.Empty, commandValue);
                    changed = true;
                }
            }

            // Computer\HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.qlplugin
            {
                // It is left to the user to choose whether to restore the default option in the future
            }

            if (changed)
            {
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero); // SHCNE_ASSOCCHANGED
            }
        }
        catch (Exception ex)
        {
            ProcessHelper.WriteLog(ex.ToString());
        }
    }

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
}
