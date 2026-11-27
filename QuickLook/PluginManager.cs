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

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using UnblockZoneIdentifier;

namespace QuickLook;

public sealed class PluginManager
{
    private static PluginManager _instance;

    private PluginManager()
    {
        var sw = Stopwatch.StartNew();
        LoadPlugins(App.UserPluginPath);
        LoadPlugins(Path.Combine(App.AppPath, @"QuickLook.Plugin\"));
        var loadMs = sw.ElapsedMilliseconds;
        sw.Restart();
        InitLoadedPlugins();
        KeyboardLog.Write($"Startup: plugins loaded in {loadMs} ms, initialized in {sw.ElapsedMilliseconds} ms");
    }

    internal IViewer DefaultPlugin { get; } = new Plugin.InfoPanel.Plugin();

    internal List<IViewer> LoadedPlugins { get; private set; } = [];

    internal static PluginManager GetInstance()
    {
        return _instance ??= new PluginManager();
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

    /// <summary>
    /// Pays the first-use costs (JIT, static constructors, dependent assemblies) at startup instead of
    /// on the first preview. Each plugin's CanHandle runs once on the UI thread at idle priority, one plugin
    /// per dispatcher operation so a key press is never held up; then every plugin assembly is compiled
    /// ahead on a low-priority background thread, which also covers Prepare and View.
    /// </summary>
    internal void WarmUp()
    {
        var sw = Stopwatch.StartNew();
        var probe = Path.Combine(Path.GetTempPath(), "QuickLook.WarmUp.nonexistent.txt");

        foreach (var plugin in LoadedPlugins)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    plugin.CanHandle(probe);
                }
                catch
                {
                    // ignored
                }
            }, DispatcherPriority.ApplicationIdle);
        }

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            KeyboardLog.Write($"Startup: CanHandle warm-up done in {sw.ElapsedMilliseconds} ms");

            var assemblies = LoadedPlugins.Select(p => p.GetType().Assembly).Distinct().ToArray();
            new Thread(() => PrepareAssemblies(assemblies))
            {
                IsBackground = true,
                Name = "QuickLook.WarmUp",
                Priority = ThreadPriority.Lowest,
            }.Start();
        }, DispatcherPriority.ApplicationIdle);
    }

    private static void PrepareAssemblies(Assembly[] assemblies)
    {
        var sw = Stopwatch.StartNew();
        var count = 0;

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = [.. e.Types.Where(t => t != null)];
            }

            foreach (var type in types)
            {
                if (type.IsGenericTypeDefinition || type.IsInterface)
                    continue;

                const BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static
                    | BindingFlags.Public | BindingFlags.NonPublic;

                MethodBase[] methods;
                try
                {
                    methods = [.. type.GetMethods(flags), .. type.GetConstructors(flags)];
                }
                catch
                {
                    continue;
                }

                foreach (var method in methods)
                {
                    if (method.IsAbstract || method.ContainsGenericParameters
                        || (method.MethodImplementationFlags & MethodImplAttributes.InternalCall) != 0
                        || method.GetMethodBody() == null)
                        continue;

                    try
                    {
                        RuntimeHelpers.PrepareMethod(method.MethodHandle);
                        count++;
                    }
                    catch
                    {
                        // a missing optional dependency, etc.: that method is simply compiled on first use
                    }
                }
            }
        }

        KeyboardLog.Write($"Startup: precompiled {count} plugin methods in {sw.ElapsedMilliseconds} ms");
    }

    private void LoadPlugins(string folder)
    {
        if (!Directory.Exists(folder))
            return;

        var failedPlugins = new List<(string Plugin, Exception Error)>();

        Directory.GetFiles(folder, "QuickLook.Plugin.*.dll", SearchOption.AllDirectories)
            .ToList()
            .ForEach(lib =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    Assembly.LoadFrom(lib)
                        .GetExportedTypes()
                        .Where(t => !t.IsInterface && !t.IsAbstract
                            && typeof(IViewer).IsAssignableFrom(t))
                        .ToArray()
                        .ForEach(t => LoadedPlugins.Add(t.CreateInstance<IViewer>()));
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
                KeyboardLog.WriteIfSlow($"startup load {Path.GetFileNameWithoutExtension(lib)}", sw.ElapsedMilliseconds);
            });

        LoadedPlugins = [.. LoadedPlugins.OrderByDescending(i => i.Priority)];

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
            var sw = Stopwatch.StartNew();
            try
            {
                i.Init();
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
            }
            KeyboardLog.WriteIfSlow($"startup init {i.GetType().Namespace}", sw.ElapsedMilliseconds);
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
            """
            Windows has blocked the plugins.
            To fix this, please follow these steps:
            1. Right-click the downloaded QuickLook zip file and select 'Properties'
            2. At the bottom of the Properties window, check 'Unblock'
            3. Click 'Apply' and 'OK'
            4. Extract the zip file again
            QuickLook will now close. Please launch it from the unblocked folder.
            """,
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

            if (!string.IsNullOrEmpty(rootDir) && Directory.Exists(rootDir))
            {
                foreach (var filePath in Directory.GetFiles(rootDir, "*.*", SearchOption.AllDirectories))
                {
                    if (ZoneIdentifierManager.IsZoneBlocked(filePath))
                    {
                        _ = ZoneIdentifierManager.UnblockZone(filePath);
                    }
                }
            }

            MessageBox.Show(
                """
                QuickLook has detected that Windows blocked the plugins, and has attempted to unblock them.
                The application will now restart to check if the unblocking was successful.
                """,
                "Security Unblock Attempt",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // Restart the application using TrayIconManager
            TrayIconManager.GetInstance().Restart(forced: true);
            return true;
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog($"Failed to perform automatic unblock: {e}");
            return false;
        }
    }
}
