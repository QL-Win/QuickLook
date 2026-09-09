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
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace QuickLook.Plugin.MediaInfoViewer;

/// <summary>
/// Renders MediaInfo output as a two-column table (field label | value) instead of a fixed-width
/// text block. The label column width is shared across all rows (Grid SharedSizeGroup), so every
/// colon-like separator lines up pixel-perfectly regardless of CJK/ASCII/full-width mixing, which
/// padding with spaces can never achieve in a proportional/font-fallback situation.
/// </summary>
public class MediaInfoTablePanel : UserControl
{
    private const string ColumnSeparator = " : ";

    private readonly StackPanel _stack;
    private string _plainText = string.Empty;

    public string Text
    {
        get => _plainText;
        set
        {
            _plainText = value ?? string.Empty;
            Rebuild();
        }
    }

    public MediaInfoTablePanel(string text, ContextObject context)
    {
        _ = context;

        _stack = new StackPanel();
        Grid.SetIsSharedSizeScope(_stack, true);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            PanningMode = PanningMode.VerticalFirst,
            Content = _stack,
        };

        // UserControl does not render its Background by default; wrap it in a Border that mirrors it.
        var border = new Border();
        border.SetBinding(Border.BackgroundProperty, new Binding(nameof(Background)) { Source = this });
        border.Child = scroll;
        Content = border;

        Margin = new Thickness(8d, 0d, 0d, 0d);
        FontSize = 14d;
        AllowDrop = true;

        FontFamily = new FontFamily("Consolas, " + TranslationHelper.Get("Editor_FontFamily",
            domain: "QuickLook.Plugin.TextViewer"));

        var copy = new MenuItem
        {
            Header = TranslationHelper.Get("Editor_Copy", domain: "QuickLook.Plugin.TextViewer"),
        };
        copy.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(_plainText);
            }
            catch
            {
                // Clipboard can be locked by another process; ignore.
            }
        };
        ContextMenu = new ContextMenu();
        ContextMenu.Items.Add(copy);

        Text = text;
    }

    private void Rebuild()
    {
        _stack.Children.Clear();

        foreach (var raw in _plainText.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.Length == 0)
                continue;

            var sep = line.IndexOf(ColumnSeparator, StringComparison.Ordinal);
            if (sep > 0)
                _stack.Children.Add(BuildFieldRow(
                    line.Substring(0, sep).Trim(),
                    line.Substring(sep + ColumnSeparator.Length)));
            else
                _stack.Children.Add(BuildSectionHeader(line));
        }
    }

    private static TextBlock BuildSectionHeader(string text) => new()
    {
        Text = text,
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(0, 8, 0, 2),
    };

    private static FrameworkElement BuildFieldRow(string label, string value)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "Label" });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });

        var labelText = new TextBlock
        {
            Text = label,
            TextAlignment = TextAlignment.Left,
            Margin = new Thickness(0, 0, 6, 0),
        };
        var colonText = new TextBlock
        {
            Text = ":",
            Margin = new Thickness(0, 0, 6, 0),
        };
        var valueText = new TextBlock
        {
            Text = value,
            TextWrapping = TextWrapping.Wrap,
        };

        Grid.SetColumn(labelText, 0);
        Grid.SetColumn(colonText, 1);
        Grid.SetColumn(valueText, 2);
        grid.Children.Add(labelText);
        grid.Children.Add(colonText);
        grid.Children.Add(valueText);

        return grid;
    }
}
