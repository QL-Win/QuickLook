// Copyright © 2024 QL-Win Contributors
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

using QuickLook.Common.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace QuickLook.Plugin.PluginInstaller;

internal static class App
{
    /// <summary>
    /// <see cref="QuickLook.App.UserPluginPath"/>
    /// </summary>
    public static string UserPluginPath
    {
        get
        {
            // Just in case
            static string Fallback() => Path.Combine(SettingHelper.LocalDataPath, "QuickLook.Plugin\\");

            try
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .First(a => a.GetName(false).Name == "QuickLook");
                var appType = loadedAssemblies?.GetType("QuickLook.App");
                var fieldInfo = appType?.GetField(nameof(UserPluginPath), BindingFlags.Public | BindingFlags.Static);

                return (fieldInfo?.GetValue(null) as string) ?? Fallback();
            }
            catch
            {
                return Fallback();
            }
        }
    }
}
