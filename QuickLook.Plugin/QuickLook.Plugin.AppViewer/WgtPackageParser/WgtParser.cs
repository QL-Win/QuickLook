// Copyright © 2017-2025 QL-Win Contributors
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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace QuickLook.Plugin.AppViewer.WgtPackageParser;

internal static class WgtParser
{
    public static WgtInfo Parse(string path)
    {
        using var fileStream = File.OpenRead(path);
        using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);
        var manifestEntry = zipArchive.GetEntry("manifest.json");

        if (manifestEntry != null)
        {
            using var stream = manifestEntry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

            if (dict == null) return null;

            var wgtInfo = new WgtInfo();

            if (dict.ContainsKey("@platforms"))
            {
                wgtInfo.Platforms = ((JArray)dict["@platforms"]).Values().Select(v => v.ToString()).ToArray();
            }
            if (dict.ContainsKey("id")) wgtInfo.AppId = dict["id"].ToString();
            if (dict.ContainsKey("name")) wgtInfo.AppName = dict["name"].ToString();
            if (dict.ContainsKey("version"))
            {
                var version = (JObject)dict["version"];

                if (version != null)
                {
                    if (version.ContainsKey("name")) wgtInfo.AppVersion = version["name"].ToString();
                    if (version.ContainsKey("code")) wgtInfo.AppVersionCode = version["code"].ToString();
                }
            }
            if (dict.ContainsKey("description")) wgtInfo.AppDescription = dict["description"].ToString();
            if (dict.ContainsKey("permissions"))
            {
                var permissions = (JObject)dict["permissions"];

                if (permissions != null)
                {
                    List<string> permissionNames = [];
                    foreach (var permission in permissions)
                    {
                        permissionNames.Add(permission.Key);
                    }
                    wgtInfo.Permissions = [.. permissionNames];
                }
            }
            if (dict.ContainsKey("plus"))
            {
                var plus = (JObject)dict["plus"];

                if (plus != null)
                {
                    if (plus.ContainsKey("locales"))
                    {
                        var locales = plus["locales"];
                        var dictionary = locales.ToObject<Dictionary<string, Dictionary<string, object>>>();

                        foreach (var locale in dictionary)
                        {
                            foreach (var kv in locale.Value)
                            {
                                if (kv.Key == "name")
                                {
                                    wgtInfo.AppNameLocales.Add(locale.Key, kv.Value.ToString());
                                }
                            }
                        }
                    }

                    if (plus.ContainsKey("uni-app"))
                    {
                        var uni_app = plus["uni-app"];
                        var dictionary = uni_app.ToObject<Dictionary<string, object>>();

                        if (dictionary.ContainsKey("vueVersion")) wgtInfo.VueVersion = dictionary["vueVersion"].ToString();
                        if (dictionary.ContainsKey("compilerVersion")) wgtInfo.CompilerVersion = dictionary["compilerVersion"].ToString();
                    }
                }
            }
            if (dict.ContainsKey("descriptionLocales"))
            {
                var descriptionLocales = (JObject)dict["descriptionLocales"];

                if (descriptionLocales != null)
                {
                    foreach (var descriptionLocale in descriptionLocales)
                    {
                        wgtInfo.AppDescriptionLocales.Add(descriptionLocale.Key, descriptionLocale.Value.ToString());
                    }
                }
            }
            if (dict.ContainsKey("fallbackLocale")) wgtInfo.FallbackLocale = dict["fallbackLocale"].ToString();

            return wgtInfo;
        }

        return null;
    }
}
