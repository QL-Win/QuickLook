using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using QuickLook.Plugin;

namespace QuickLook
{
    internal class PluginManager
    {
        private static PluginManager _instance;

        internal PluginManager()
        {
            LoadPlugins();
        }

        internal List<IViewer> LoadedPlugins { get; } = new List<IViewer>();

        internal static PluginManager GetInstance()
        {
            return _instance ?? (_instance = new PluginManager());
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
                            .ForEach(type => LoadedPlugins.Add((IViewer) Activator.CreateInstance(type)));
                    });
        }
    }
}