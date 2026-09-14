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

using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Common.Plugin.MoreMenu;
using QuickLook.MediaInfo;
using QuickLook.MediaInfo.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuickLook.Plugin.MediaInfoViewer;

public sealed partial class Plugin : IViewer, IMoreMenuExtended
{
    private MediaInfoTablePanel _tvp;

    public int Priority => int.MinValue;

    public IEnumerable<IMenuItem> MenuItems => GetMenuItems();

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        // We only handle files with specific caller
        return false;
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 800, Height = 600 };
    }

    public void View(string path, ContextObject context)
    {
        _tvp = new MediaInfoTablePanel(Inform(path), context);
        AssignHighlightingManager(_tvp, context);

        _tvp.Tag = context;
        _tvp.Drop += OnDrop;

        context.ViewerContent = _tvp;
        context.Title = $"{Path.GetFileName(path)}";
        context.IsBusy = false;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files
                && files.FirstOrDefault() is string path)
            {
                if (_tvp!.Tag is ContextObject context)
                {
                    context.Title = $"{Path.GetFileName(path)}";
                }

                _tvp!.Text = Inform(path);
            }
        }
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _tvp = null!;
    }

    private static string Inform(string path)
    {
        using MediaInfoNative lib = new();

        if (CultureInfo.CurrentUICulture.Name.ToLowerInvariant() switch
            {
                "zh-cn" or "zh-hans" or "zh" => MediaInfoLanguage.ChineseSimplified,
                "zh-tw" or "zh-hk" or "zh-hant" => MediaInfoLanguage.ChineseTraditional,
                "ja" => MediaInfoLanguage.Japanese,
                "ko" => MediaInfoLanguage.Korean,
                "fr" => MediaInfoLanguage.French,
                "de" => MediaInfoLanguage.German,
                "es" => MediaInfoLanguage.Spanish,
                "it" => MediaInfoLanguage.Italian,
                "ru" => MediaInfoLanguage.Russian,
                "pt-br" => MediaInfoLanguage.PortugueseBrazilian,
                "pt" => MediaInfoLanguage.Portuguese,
                "nl" => MediaInfoLanguage.Dutch,
                "sv" => MediaInfoLanguage.Swedish,
                "pl" => MediaInfoLanguage.Polish,
                "cs" => MediaInfoLanguage.Czech,
                "ar" => MediaInfoLanguage.Arabic,
                _ => (MediaInfoLanguage?)null,
            } is MediaInfoLanguage lang)
        {
            var csv = lang.ToIso639();
            if (!string.IsNullOrEmpty(csv))
                lib.Option("Language", csv);
        }

        lib.Open(path);
        return lib.Inform();
    }

    private void AssignHighlightingManager(Control panel, ContextObject context)
    {
        var isDark = OSThemeHelper.AppsUseDarkTheme();

        if (isDark)
        {
            context.Theme = Themes.Dark;
            panel.Background = Brushes.Transparent;
            panel.SetResourceReference(Control.ForegroundProperty, "WindowTextForeground");
        }
        else
        {
            context.Theme = Themes.Light;
            panel.Background = OSThemeHelper.AppsUseDarkTheme()
                ? new SolidColorBrush(Color.FromArgb(175, 255, 255, 255))
                : Brushes.Transparent;
        }
    }
}
