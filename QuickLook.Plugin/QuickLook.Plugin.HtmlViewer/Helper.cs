using System.IO;
using System.Text;
using Microsoft.Win32;

namespace QuickLook.Plugin.HtmlViewer
{
    internal static class Helper
    {
        public static string FilePathToFileUrl(string filePath)
        {
            var uri = new StringBuilder();
            foreach (var v in filePath)
                if (v >= 'a' && v <= 'z' || v >= 'A' && v <= 'Z' || v >= '0' && v <= '9' ||
                    v == '+' || v == '/' || v == ':' || v == '.' || v == '-' || v == '_' || v == '~' ||
                    v > '\xFF')
                    uri.Append(v);
                else if (v == Path.DirectorySeparatorChar || v == Path.AltDirectorySeparatorChar)
                    uri.Append('/');
                else
                    uri.Append($"%{(int) v:X2}");
            if (uri.Length >= 2 && uri[0] == '/' && uri[1] == '/') // UNC path
                uri.Insert(0, "file:");
            else
                uri.Insert(0, "file:///");
            return uri.ToString();
        }

        public static void SetBrowserFeatureControl()
        {
            var exeName = Path.GetFileName(App.AppFullPath);

            // use latest engine
            SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", exeName, 0);
            //
            SetBrowserFeatureControlKey("FEATURE_GPU_RENDERING", exeName, 0);
            // turn on hi-dpi mode
            SetBrowserFeatureControlKey("FEATURE_96DPI_PIXEL", exeName, 1);
        }

        private static void SetBrowserFeatureControlKey(string feature, string appName, uint value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(
                string.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\", feature),
                RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                key?.SetValue(appName, value, RegistryValueKind.DWord);
            }
        }
    }
}