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

using System.Collections.Generic;
using System.Globalization;

namespace QuickLook.Plugin.AppViewer.WgtPackageParser;

public sealed class WgtInfo
{
    /// <summary>
    /// Json path: @platforms
    /// </summary>
    public string[] Platforms { get; set; }

    /// <summary>
    /// Json path: id
    /// </summary>
    public string AppId { get; set; }

    /// <summary>
    /// Json path: name
    /// </summary>
    public string AppName { get; set; }

    /// <summary>
    /// Json path: plus.locales.xxx.name
    /// </summary>
    public Dictionary<string, string> AppNameLocales { get; set; } = [];

    public string AppNameLocale
    {
        get
        {
            try
            {
                CultureInfo culture = CultureInfo.CurrentCulture;

                while (!AppNameLocales.ContainsKey(culture.Name))
                {
                    if (culture.Parent == CultureInfo.InvariantCulture)
                    {
                        culture = CultureInfo.InvariantCulture;
                        break;
                    }
                    culture = culture.Parent;
                }

                return AppNameLocales[culture.Name];
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Json path: plus.uni-app.vueVersion
    /// </summary>
    public string VueVersion { get; set; }

    /// <summary>
    /// Json path: plus.uni-app.compilerVersion
    /// </summary>
    public string CompilerVersion { get; set; }

    /// <summary>
    /// Json path: versionName.name
    /// </summary>
    public string AppVersionName { get; set; }

    /// <summary>
    /// Json path: versionName.code
    /// </summary>
    public string AppVersionCode { get; set; }

    /// <summary>
    /// Json path: description
    /// </summary>
    public string AppDescription { get; set; }

    /// <summary>
    /// Json path: descriptionLocales
    /// </summary>
    public Dictionary<string, string> AppDescriptionLocales { get; set; } = [];

    /// <summary>
    /// Json path: descriptionLocales
    /// </summary>
    public string[] Permissions { get; set; }

    /// <summary>
    /// Json path: fallbackLocale
    /// </summary>
    public string FallbackLocale { get; set; }
}
