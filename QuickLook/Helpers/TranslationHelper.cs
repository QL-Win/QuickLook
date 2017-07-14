using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.XPath;

namespace QuickLook.Helpers
{
    internal class TranslationHelper
    {
        private static readonly CultureInfo CurrentCultureInfo = CultureInfo.CurrentUICulture;
        //private static readonly CultureInfo CurrentCultureInfo = CultureInfo.GetCultureInfo("zh-CN");

        private static readonly Dictionary<string, XPathNavigator> FileCache = new Dictionary<string, XPathNavigator>();
        private static readonly Dictionary<string, string> StringCache = new Dictionary<string, string>();

        public static string GetString(string file, string id, CultureInfo locale = null, string failsafe = null)
        {
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
            var cacheKey = $"{locale.Name}::{id}";
            if (StringCache.ContainsKey(cacheKey))
                return StringCache[cacheKey];

            var result = nav.SelectSingleNode($@"/Translations/{locale.Name}/{id}");
            StringCache.Add(cacheKey, result?.Value);

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
}