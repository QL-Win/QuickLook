using System.IO;
using System.Linq;
using QuickLook.Plugin;

namespace QuickLook
{
    internal static class PathToPluginMatcher
    {
        internal static IViewer FindMatch(string[] paths)
        {
            if (paths.Length == 0)
                return null;

            //TODO: Handle multiple files?
            var path = paths.First();

            return FindByExtension(path) ?? FindByContent(path);
        }

        private static IViewer FindByExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var ext = Path.GetExtension(path).ToLower();

            return PluginManager.GetInstance()
                .LoadedPlugins.FirstOrDefault(plugin =>
                {
                    if ((plugin.Type & PluginType.ByExtension) == 0)
                        return false;

                    return plugin.SupportExtensions.Any(e => e == ext);
                });
        }

        private static IViewer FindByContent(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            byte[] sample;
            using (var br = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                sample = br.ReadBytes(256);
            }

            return PluginManager.GetInstance()
                .LoadedPlugins.FirstOrDefault(plugin =>
                {
                    if ((plugin.Type & PluginType.ByContent) == 0)
                        return false;

                    return plugin.CheckSupportByContent(sample);
                });
        }
    }
}