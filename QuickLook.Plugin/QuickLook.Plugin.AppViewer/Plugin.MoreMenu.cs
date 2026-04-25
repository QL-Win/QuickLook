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
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace QuickLook.Plugin.AppViewer;

public sealed partial class Plugin
{
    public IEnumerable<IMenuItem> GetMenuItems()
    {
        if (string.IsNullOrEmpty(_path))
            yield break;

        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");

        if (_path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".apk.1", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".aab", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".ipa", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".hap", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".hap.1", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".appx", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".appxbundle", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".msix", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".msixbundle", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".snupkg", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".wgt", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".wgtu", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".aar", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".har", StringComparison.OrdinalIgnoreCase))
        {
            // If the file is ZIP-based (PK..), allow reopening with ArchiveViewer
            yield return new MoreMenuItem()
            {
                Icon = FontSymbols.Tablet,
                Header = TranslationHelper.Get("MW_OpenInArchiveViewer", translationFile),
                MenuItems = null,
                Command = new RelayCommand(() => PluginHelper.InvokePluginPreview("QuickLook.Plugin.ArchiveViewer", _path)),
            };
        }
    }
}
