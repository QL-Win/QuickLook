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

        internal static string GetUrlPath(string url)
        {
            int index = -1;
            string[] lines = File.ReadAllLines(url);
            foreach (string line in lines)
            {
                if (line.ToLower().Contains("url="))
                {
                    index = System.Array.IndexOf(lines, line);
                    break;
                }
            }
            if (index != -1)
            {
                var fullLine = lines.GetValue(index);
                return fullLine.ToString().Substring(fullLine.ToString().LastIndexOf('=') + 1);
            }
            return url;
        }
    }
}