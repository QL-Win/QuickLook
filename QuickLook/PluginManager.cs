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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;

namespace QuickLook
{
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

            try
            {
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
                                    catch (FileLoadException ex) when (ex.Message.Contains("0x80131515") && SettingHelper.IsPortableVersion())
                                    {
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
                                        throw;
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
            catch (Exception ex)
            {
                ProcessHelper.WriteLog(ex.ToString());
                throw;
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
    }
}