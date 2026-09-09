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
using System.Text;
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
        using MediaInfoNative lib = new();
        lib.Open(path);
        ApplyLanguage(lib);

        _tvp = new MediaInfoTablePanel(LocalizeExeFields(lib.Inform()), context);
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

                using MediaInfoNative lib = new();
                lib.Open(path);
                ApplyLanguage(lib);
                _tvp!.Text = LocalizeExeFields(lib.Inform());
            }
        }
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _tvp = null!;
    }

    private static void ApplyLanguage(MediaInfoNative lib)
    {
        // Match MediaInfo's output language to QuickLook's UI language so field labels render in
        // the user's language. Unsupported cultures fall back to MediaInfo's default (English).
        var lang = CultureInfo.CurrentUICulture.Name.ToLowerInvariant() switch
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
        };

        if (lang is MediaInfoLanguage value)
        {
            var csv = value.ToIso639();
            if (!string.IsNullOrEmpty(csv))
                lib.Option("Language", csv);
        }
    }

    private const string ColumnSeparator = " : ";

    /// <summary>
    /// The zh-CN language CSV shipped with MediaInfo is missing several PE/EXE field labels, so the
    /// library falls back to full-width Latin (e.g. Ｌｉｎｋｅｒ＿Ｖｅｒｓｉｏｎ). Normalize those
    /// labels back to half-width (NFKC), translate the missing fields, then re-align the columns.
    /// </summary>
    private static readonly Dictionary<string, string> MissingExeFieldTranslations = new()
    {
        ["Linker_Version"] = "链接器版本",
        ["Subsystem_Name"] = "子系统名称",
        ["Subsystem_Version"] = "子系统版本",
        ["Machine_Type"] = "机器类型",
        ["Entry_Point"] = "入口点",
        ["Image_Version"] = "映像版本",
        ["OS_Version"] = "操作系统版本",
        ["Code_Size"] = "代码大小",
        ["Initialized_Data_Size"] = "已初始化数据大小",
        ["Uninitialized_Data_Size"] = "未初始化数据大小",
        ["Time_Stamp"] = "时间戳",
        ["TimeStamp"] = "时间戳",
    };

    private static string LocalizeExeFields(string inform)
    {
        if (string.IsNullOrEmpty(inform))
            return inform;

        var lines = inform.Replace("\r\n", "\n").Split('\n');
        var sb = new StringBuilder(inform.Length);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var sep = line.IndexOf(ColumnSeparator, StringComparison.Ordinal);
            if (sep > 0)
            {
                var label = line.Substring(0, sep).TrimEnd();
                var value = line.Substring(sep + ColumnSeparator.Length);

                // Normalize full-width fallback labels (Ｌｉｎｋｅｒ＿Ｖｅｒｓｉｏｎ → Linker_Version).
                var normalized = label.Normalize(NormalizationForm.FormKC);
                // Translate missing PE field names, and the nested (Profile) token.
                normalized = normalized.Replace("(Profile)", "(配置)");
                if (MissingExeFieldTranslations.TryGetValue(normalized, out var translated))
                    normalized = translated;

                sb.Append(normalized).Append(ColumnSeparator).Append(value);
            }
            else
            {
                sb.Append(line);
            }

            if (i < lines.Length - 1)
                sb.Append('\n');
        }

        return sb.ToString();
    }

    private void AssignHighlightingManager(Control panel, ContextObject context)
    {
        var isDark = OSThemeHelper.AppsUseDarkTheme();

        if (isDark)
        {
            context.Theme = Themes.Dark;
            panel.Background = Brushes.Transparent;
            panel.SetResourceReference(TextBlock.ForegroundProperty, "WindowTextForeground");
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
