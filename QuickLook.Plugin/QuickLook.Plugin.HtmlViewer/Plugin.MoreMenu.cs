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

using QuickLook.Common.Commands;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin.MoreMenu;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Input;

namespace QuickLook.Plugin.HtmlViewer;

public partial class Plugin
{
    public ICommand ViewSourceCodeCommand { get; }

    public Plugin()
    {
        ViewSourceCodeCommand = new RelayCommand(ViewSourceCode);
    }

    public IEnumerable<IMenuItem> GetMenuItems()
    {
        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");

        yield return new MoreMenuItem()
        {
            Icon = "\uE943",
            Header = TranslationHelper.Get("MW_ViewSourceCode", translationFile),
            MenuItems = null,
            Command = ViewSourceCodeCommand,
        };
    }

    public void ViewSourceCode()
    {
        PluginHelper.InvokePluginPreview("QuickLook.Plugin.TextViewer", _currentPath);
    }
}
