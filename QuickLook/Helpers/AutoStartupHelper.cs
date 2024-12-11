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
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using QuickLook.Common.Helpers;
using QuickLook.NativeMethods;

namespace QuickLook.Helpers;

internal static class AutoStartupHelper
{
    private static readonly string StartupFullPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        Path.ChangeExtension(Path.GetFileName(App.AppFullPath), ".lnk"));

    internal static void CreateAutorunShortcut()
    {
        if (App.IsUWP)
            return;

        try
        {
            RemoveAutorunShortcut();

            var lnk = (IShellLinkW)new ShellLink();

            lnk.SetPath(App.AppFullPath);
            lnk.SetArguments("/autorun"); // silent
            lnk.SetIconLocation(App.AppFullPath, 0);
            lnk.SetWorkingDirectory(App.AppPath);
            ((IPersistFile)lnk).Save(StartupFullPath, false);
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
            TrayIconManager.ShowNotification("", "Failed to add QuickLook to Startup folder.");
        }
    }

    internal static void RemoveAutorunShortcut()
    {
        if (App.IsUWP)
            return;

        try
        {
            File.Delete(StartupFullPath);
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
            TrayIconManager.ShowNotification("", "Failed to delete QuickLook startup shortcut.");
        }
    }

    internal static bool IsAutorun()
    {
        if (App.IsUWP)
            return true;

        return File.Exists(StartupFullPath);
    }
}
