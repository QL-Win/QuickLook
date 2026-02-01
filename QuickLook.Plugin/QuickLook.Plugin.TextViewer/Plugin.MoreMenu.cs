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

using QuickLook.Common.Commands;
using QuickLook.Common.Controls;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin.MoreMenu;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace QuickLook.Plugin.TextViewer;

public sealed partial class Plugin
{
    public IEnumerable<IMenuItem> GetMenuItems()
    {
        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        string extension = "";
        try
        {
            extension = Path.GetExtension(_currentPath).ToLower();
        } catch (System.Exception)
        { 
        }
            

        var reopen = extension switch
        {
            // HTML <=> HTML TEXT
            // \QuickLook\QuickLook.Plugin\QuickLook.Plugin.TextViewer\Syntax\Light\zzz-After-JavaScript-HTML.xshd
            // .html;.htm;.xhtml;.shtml;.shtm;.xht;.hta
            ".html" or ".htm" or ".xhtml" or ".shtml" or ".shtm" or ".xht" or ".hta" => new MoreMenuItem()
            {
                Icon = FontSymbols.Globe,
                Header = TranslationHelper.Get("MW_ReopenAsHtmlPreview", translationFile),
                Command = new RelayCommand(() => PluginHelper.InvokePluginPreview("QuickLook.Plugin.HtmlViewer", _currentPath)),
            },

            // SVG IMAGE <=> XML TEXT
            ".svg" => new MoreMenuItem()
            {
                Icon = FontSymbols.Picture,
                Header = TranslationHelper.Get("MW_ReopenAsImagePreview", translationFile),
                Command = new RelayCommand(() => PluginHelper.InvokePluginPreview("QuickLook.Plugin.ImageViewer", _currentPath)),
            },
            _ => null,
        };

        if (reopen is not null)
            yield return reopen;
    }
}
