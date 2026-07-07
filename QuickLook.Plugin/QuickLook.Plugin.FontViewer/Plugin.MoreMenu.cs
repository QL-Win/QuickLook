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

namespace QuickLook.Plugin.FontViewer;

public sealed partial class Plugin
{
    private MoreMenuItem _iconFontMenuItem;
    private MoreMenuItem _pangramMenuItem;

    public IEnumerable<IMenuItem> GetMenuItems()
    {
        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");

        _iconFontMenuItem ??= new MoreMenuItem()
        {
            Icon = FontSymbols.GridView,
            Header = TranslationHelper.Get("MW_ReopenAsIconFontPreview", translationFile),
            Command = new RelayCommand(() => SwitchPreviewMode(PreviewMode.IconFont)),
        };

        _pangramMenuItem ??= new MoreMenuItem()
        {
            Icon = FontSymbols.Font,
            Header = TranslationHelper.Get("MW_ReopenAsFontPreview", translationFile),
            Command = new RelayCommand(() => SwitchPreviewMode(PreviewMode.Pangram)),
        };

        UpdateMenuVisibility();

        yield return _iconFontMenuItem;
        yield return _pangramMenuItem;
    }

    private void SwitchPreviewMode(PreviewMode mode)
    {
        if (_previewMode == mode)
            return;

        _previewMode = mode;
        SettingHelper.Set("LastPreviewMode", (int)mode, ConfigDomain);
        UpdateMenuVisibility();
        ApplyPreviewMode(_currentPath);
    }

    private void UpdateMenuVisibility()
    {
        if (_iconFontMenuItem is null || _pangramMenuItem is null)
            return;

        _iconFontMenuItem.IsVisible = _previewMode != PreviewMode.IconFont;
        _pangramMenuItem.IsVisible = _previewMode == PreviewMode.IconFont;
    }
}
