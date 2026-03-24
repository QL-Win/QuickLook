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

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml.XPath;

namespace QuickLook.Common.Helpers;

public static class TranslationHelper
{
    private static readonly CultureInfo CurrentCultureInfo = CultureInfo.CurrentUICulture;

    private static readonly Dictionary<string, XPathNavigator> FileCache = [];

    public static string Get(string id, string file = null, CultureInfo locale = null, string failsafe = null,
        string domain = "QuickLook")
    {
        if (file == null)
        {
            var subDir = domain == "QuickLook" ? string.Empty : $"QuickLook.Plugin\\{domain}";
            file = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), // path of QuickLook.Common.dll
                subDir, "Translations.config");
        }

        if (!File.Exists(file))
            return failsafe ?? id;

        if (locale == null)
            locale = CurrentCultureInfo;

        var nav = GetLangFile(file);

        // try to get string
        var s = GetStringFromXml(nav, id, locale);
        if (s != null)
            return s;

        // try again for parent language
        if (locale.Parent.Name != string.Empty)
            s = GetStringFromXml(nav, id, locale.Parent);
        if (s != null)
            return s;

        // use fallback language
        s = GetStringFromXml(nav, id, CultureInfo.GetCultureInfo("en"));
        if (s != null)
            return s;

        return failsafe ?? id;
    }

    private static string GetStringFromXml(XPathNavigator nav, string id, CultureInfo locale)
    {
        var result = nav.SelectSingleNode($@"/Translations/{locale.Name}/{id}");

        return result?.Value;
    }

    private static XPathNavigator GetLangFile(string file)
    {
        if (FileCache.ContainsKey(file))
            return FileCache[file];

        var res = new XPathDocument(file).CreateNavigator();
        FileCache.Add(file, res);
        return res;
    }
}
