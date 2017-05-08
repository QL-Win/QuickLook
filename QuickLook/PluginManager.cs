using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        internal Type DefaultPlugin { get; } = typeof(PluginInterface);

        internal List<Type> LoadedPlugins { get; private set; } = new List<Type>();

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
                        can = plugin.CreateInstance<IViewer>().CanHandle(path);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                    return can;
                })
                ?.CreateInstance<IViewer>();

            return matched ?? DefaultPlugin.CreateInstance<IViewer>();
        }

        private void LoadPlugins()
        {
            Directory.GetFiles(Path.Combine(App.AppPath, "Plugins\\"), "QuickLook.Plugin.*.dll",
                    SearchOption.AllDirectories)
                .ToList()
                .ForEach(
                    lib =>
                    {
                        (from t in Assembly.LoadFrom(lib).GetExportedTypes()
                                where !t.IsInterface && !t.IsAbstract
                                where typeof(IViewer).IsAssignableFrom(t)
                                select t).ToList()
                            .ForEach(type => LoadedPlugins.Add(type));
                    });

            LoadedPlugins = LoadedPlugins.OrderByDescending(i => i.CreateInstance<IViewer>().Priority).ToList();
        }
    }
}