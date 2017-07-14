using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.XPath;

namespace QuickLook.Helpers
{
    internal class TranslationHelper
    {
        private static readonly CultureInfo CurrentCultureInfo = CultureInfo.CurrentUICulture;
        //private static readonly CultureInfo CurrentCultureInfo = CultureInfo.GetCultureInfo("zh-CN");

        private static readonly Dictionary<string, XPathNavigator> FileCache = new Dictionary<string, XPathNavigator>();
        private static readonly Dictionary<string, string> StringCache = new Dictionary<string, string>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetString(string id, string file = null, CultureInfo locale = null, string failsafe = null,
            Assembly calling = null)
        {
            if (file == null)
                file = Path.Combine(Path.GetDirectoryName((calling ?? Assembly.GetCallingAssembly()).Location),
                    "Translations.config");

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
            var cacheKey = $"{nav.BaseURI.GetHashCode()}::{locale.Name}::{id}";
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