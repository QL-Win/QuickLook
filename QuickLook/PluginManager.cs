using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using QuickLook.ExtensionMethods;
using QuickLook.Plugin;
using QuickLook.Plugin.InfoPanel;

namespace QuickLook
{
    internal class PluginManager
    {
        private static PluginManager _instance;

        private PluginManager()
        {
            LoadPlugins();
        }

        internal IViewer DefaultPlugin { get; } = new PluginInterface();

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
                        can = plugin.CanHandle(path);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                    return can;
                });

            return matched ?? DefaultPlugin;
        }

        private void LoadPlugins()
        {
            Directory.GetFiles(Path.Combine(App.AppPath, "QuickLook.Plugin\\"), "QuickLook.Plugin.*.dll",
                    SearchOption.AllDirectories)
                .ToList()
                .ForEach(
                    lib =>
                    {
                        (from t in Assembly.LoadFrom(lib).GetExportedTypes()
                                where !t.IsInterface && !t.IsAbstract
                                where typeof(IViewer).IsAssignableFrom(t)
                                select t).ToList()
                            .ForEach(type => LoadedPlugins.Add(type.CreateInstance<IViewer>()));
                    });

            LoadedPlugins = LoadedPlugins.OrderByDescending(i => i.Priority).ToList();

            LoadedPlugins.ForEach(i =>
            {
                try
                {
                    i.Init();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            });
        }
    }
}