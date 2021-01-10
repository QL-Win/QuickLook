// Copyright © 2021 Paddy Xu and Frank Becker
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
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace QuickLook.Plugin.HtmlViewer
{
    internal static class Helper
    {
        public static bool IsWebView2Available()
        {
            var path = App.Is64Bit
                ? @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}\"
                : @"SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}\";

            using (var key = Registry.LocalMachine.OpenSubKey(path, RegistryKeyPermissionCheck.ReadSubTree))
            {
                var pv = key?.GetValue("pv");
                return !string.IsNullOrEmpty(pv as string);
            }
        }

        public static Uri FilePathToFileUrl(string filePath)
        {
            var uri = new StringBuilder();
            foreach (var v in filePath)
                if (v >= 'a' && v <= 'z' || v >= 'A' && v <= 'Z' || v >= '0' && v <= '9' ||
                    v == '+' || v == '/' || v == ':' || v == '.' || v == '-' || v == '_' || v == '~' ||
                    v > '\x80')
                    uri.Append(v);
                else if (v == Path.DirectorySeparatorChar || v == Path.AltDirectorySeparatorChar)
                    uri.Append('/');
                else
                    uri.Append($"%{(int) v:X2}");
            if (uri.Length >= 2 && uri[0] == '/' && uri[1] == '/') // UNC path
                uri.Insert(0, "file:");
            else
                uri.Insert(0, "file:///");

            try
            {
                return new Uri(uri.ToString());
            }
            catch
            {
                return null;
            }
        }

        internal static string GetUrlPath(string url)
        {
            var index = -1;
            var lines = File.ReadAllLines(url);
            foreach (var line in lines)
                if (line.ToLower().Contains("url="))
                {
                    index = Array.IndexOf(lines, line);
                    break;
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