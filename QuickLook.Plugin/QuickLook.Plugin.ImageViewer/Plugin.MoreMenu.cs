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

namespace QuickLook.Plugin.ImageViewer;

public partial class Plugin
{
    public IEnumerable<IMenuItem> GetMenuItems()
    {
        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        string extension = Path.GetExtension(_currentPath).ToLower();

        var reopen = extension switch
        {
            // SVG IMAGE <=> XML TEXT
            ".svg" => new MoreMenuItem()
            {
                Icon = FontSymbols.Code,
                Header = TranslationHelper.Get("MW_ReopenAsSourceCode", translationFile),
                Command = new RelayCommand(() => PluginHelper.InvokePluginPreview("QuickLook.Plugin.TextViewer", _currentPath)),
            },
            _ => null,
        };

        if (reopen is not null)
            yield return reopen;
    }
}
