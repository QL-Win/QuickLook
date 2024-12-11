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

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace QuickLook;

internal class PluginManager
{
    private static PluginManager _instance;

    private PluginManager()
    {
        LoadPlugins(App.UserPluginPath);
        LoadPlugins(Path.Combine(App.AppPath, "QuickLook.Plugin\\"));
        InitLoadedPlugins();
    }

    internal IViewer DefaultPlugin { get; } = new Plugin.InfoPanel.Plugin();

    internal List<IViewer> LoadedPlugins { get; private set; } = new List<IViewer>();

    internal static PluginManager GetInstance()
    {
        return _instance ?? (_instance = new PluginManager());
    }

    internal IViewer FindMatch(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var matched = GetInstance()
            .LoadedPlugins.FirstOrDefault(plugin =>
            {
                var can = false;
                try
                {
                    var timer = new Stopwatch();
                    timer.Start();

                    can = plugin.CanHandle(path);

                    timer.Stop();
                    Debug.WriteLine($"{plugin.GetType()}: {can}, {timer.ElapsedMilliseconds}ms");
                }
                catch (Exception)
                {
                    // ignored
                }

                return can;
            });

        return (matched ?? DefaultPlugin).GetType().CreateInstance<IViewer>();
    }

    private void LoadPlugins(string folder)
    {
        if (!Directory.Exists(folder))
            return;

        var failedPlugins = new List<(string Plugin, Exception Error)>();

        Directory.GetFiles(folder, "QuickLook.Plugin.*.dll",
                SearchOption.AllDirectories)
            .ToList()
            .ForEach(
                lib =>
                        {
                            try
                            {
                                (from t in Assembly.LoadFrom(lib).GetExportedTypes()
                                 where !t.IsInterface && !t.IsAbstract
                                 where typeof(IViewer).IsAssignableFrom(t)
                                 select t).ToList()
                                .ForEach(type => LoadedPlugins.Add(type.CreateInstance<IViewer>()));
                            }
                            // 0x80131515: ERROR_ASSEMBLY_FILE_BLOCKED - Windows blocked the assembly due to security policy
                            catch (FileLoadException ex) when (ex.HResult == unchecked((int)0x80131515) && SettingHelper.IsPortableVersion())
                            {
                                if (!HandleSecurityBlockedException()) throw;
                            }
                            catch (Exception ex)
                            {
                                // Log the error
                                ProcessHelper.WriteLog($"Failed to load plugin {Path.GetFileName(lib)}: {ex}");
                                failedPlugins.Add((Path.GetFileName(lib), ex));
                            }
                        });

        LoadedPlugins = LoadedPlugins.OrderByDescending(i => i.Priority).ToList();

        // If any plugins failed to load, show a message box with the details
        if (failedPlugins.Any())
        {
            var message = "The following plugins failed to load:\n\n" +
                string.Join("\n", failedPlugins.Select(f => $"• {f.Plugin}"));

            MessageBox.Show(
                message,
                "Some Plugins Failed to Load",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void InitLoadedPlugins()
    {
        LoadedPlugins.ForEach(i =>
        {
            try
            {
                i.Init();
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
            }
        });
    }

    /// <summary>
    /// Handles the case when Windows has blocked plugin files due to security policy.
    /// Attempts automatic unblock first, then shows manual instructions if that fails.
    /// </summary>
    /// <returns>
    /// <para>true if automatic unblock succeeded and app is restarting.</para>
    /// <para>false if manual intervention is needed and exception should be thrown.</para>
    /// </returns>
    private static bool HandleSecurityBlockedException()
    {
        var triedUnblock = SettingHelper.Get("TriedUnblock", false);
        if (!triedUnblock)
        {
            SettingHelper.Set("TriedUnblock", true);
            if (TryUnblockFilesAndRestart()) return true;
        }

        // Show manual unblock instructions if automatic unblock failed or was already attempted
        MessageBox.Show(
            "Windows has blocked the plugins.\n\n" +
            "To fix this, please follow these steps:\n" +
            "1. Right-click the downloaded QuickLook zip file and select 'Properties'\n" +
            "2. At the bottom of the Properties window, check 'Unblock'\n" +
            "3. Click 'Apply' and 'OK'\n" +
            "4. Extract the zip file again\n\n" +
            "QuickLook will now close. Please launch it from the unblocked folder.",
            "Security Block Detected",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        return false;
    }

    /// <summary>
    /// Attempts to automatically unblock all files in the application directory using PowerShell's Unblock-File cmdlet.
    /// If successful, restarts the application to apply the changes.
    /// </summary>
    /// <returns>
    /// <para>true if the unblock command succeeded and application restart was initiated.</para>
    /// <para>false if the unblock command failed, in which case manual unblock instructions should be shown.</para>
    /// </returns>
    private static bool TryUnblockFilesAndRestart()
    {
        ProcessHelper.WriteLog("Attempting automatic unblock of plugins...");

        try
        {
            var rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (rootDir != null)
            {
                // Create and start PowerShell process
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Get-ChildItem '{rootDir}' -Recurse | Unblock-File\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit();
                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();

                        if (!string.IsNullOrEmpty(error))
                            ProcessHelper.WriteLog($"PowerShell unblock output error: {error}");
                        if (!string.IsNullOrEmpty(output))
                            ProcessHelper.WriteLog($"PowerShell unblock output: {output}");
                    }
                }
            }

            MessageBox.Show(
                "QuickLook has detected that Windows blocked the plugins, and has attempted to unblock them.\n\n" +
                "The application will now restart to check if the unblocking was successful.",
                "Security Unblock Attempt",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Restart the application using TrayIconManager
            TrayIconManager.GetInstance().Restart(forced: true);
            return true;
        }
        catch (Exception unblockEx)
        {
            ProcessHelper.WriteLog($"Failed to perform automatic unblock: {unblockEx}");
            return false;
        }
    }
}
