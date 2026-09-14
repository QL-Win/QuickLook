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
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace QuickLook.Plugin.MediaInfoViewer;

/// <summary>
/// Read-only RichTextBox hosting a <see cref="TabFlowDocument"/> so MediaInfo output keeps
/// native FlowDocument selection while still aligning label/value columns on tab stops.
/// </summary>
public class MediaInfoTablePanel : RichTextBox
{
    private readonly TabFlowDocument _document;
    private string _plainText = string.Empty;
    private bool _loaded;

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

        _document = new TabFlowDocument();
        _document.SetBinding(TextElement.ForegroundProperty,
            new Binding(nameof(Foreground)) { Source = this });
        Document = _document;

        IsReadOnly = true;
        IsUndoEnabled = false;
        IsDocumentEnabled = true;
        IsReadOnlyCaretVisible = true;
        AcceptsReturn = true;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        BorderThickness = new Thickness(0d);
        Margin = new Thickness(8d, 0d, 8d, 0d);
        Padding = new Thickness(0d, 4d, 0d, 4d);
        FontSize = 14d;
        AllowDrop = true;
        IsManipulationEnabled = true;

        FontFamily = new FontFamily(TranslationHelper.Get("Editor_FontFamily",
            domain: "QuickLook.Plugin.TextViewer"));

        ContextMenu = new ContextMenu();
        ContextMenu.Items.Add(new MenuItem
        {
            Header = TranslationHelper.Get("Editor_Copy", domain: "QuickLook.Plugin.TextViewer"),
            Command = ApplicationCommands.Copy,
        });
        ContextMenu.Items.Add(new MenuItem
        {
            Header = TranslationHelper.Get("Editor_SelectAll",
                domain: "QuickLook.Plugin.TextViewer"),
            Command = ApplicationCommands.SelectAll,
        });

        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;

        Text = text;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _loaded = true;
        SyncMetrics();
        Rebuild();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!_loaded)
            return;

        SyncMetrics();
    }

    private void SyncMetrics()
    {
        var dip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var dipChanged = Math.Abs(_document.PixelsPerDip - dip) > 0.01;
        _document.PixelsPerDip = dip;
        _document.FontFamily = FontFamily;
        _document.FontSize = FontSize;

        var width = ActualWidth - Padding.Left - Padding.Right - 8d;
        if (width > 32d && (double.IsNaN(_document.PageWidth) || Math.Abs(_document.PageWidth - width) > 1d))
            _document.PageWidth = width;

        if (dipChanged && _loaded)
            Rebuild();
    }

    private void Rebuild()
    {
        _document.FontFamily = FontFamily;
        _document.FontSize = FontSize;
        _document.LoadKeyedLines(_plainText);
    }
}

/// <summary>
/// FlowDocument that implements tab stops the way WPF <see cref="TextParagraphProperties"/>
/// and WinUI 3 Line Services do, without using FlowDocument.Table (whose Star columns collapse
/// inside RichTextBox).
/// <para>
/// WPF (TextParagraphProperties.DefaultIncrementalTab): 4 em of the paragraph default font.
/// WinUI 3 (LineServicesFetchTabs): a single repeating tab whose width is 4 × the default run.
/// Explicit stops use <see cref="TextTabProperties"/> (Left / Center / Right / Character).
/// </para>
/// FlowDocument.Paragraph has no Tabs API, so each '\t' is expanded to an
/// <see cref="InlineUIContainer"/> whose width is the remaining distance to the next stop.
/// Text stays in <see cref="Run"/>s, so RichTextBox selection/copy still work.
/// </summary>
public class TabFlowDocument : FlowDocument
{
    public const string DefaultColumnSeparator = " : ";

    private readonly List<TextTabProperties> _tabStops = new();

    /// <summary>
    /// Explicit tab stops, sorted by <see cref="TextTabProperties.Location"/> when resolving.
    /// Empty means fall back to <see cref="IncrementalTab"/> (WPF incremental tabs).
    /// </summary>
    public IList<TextTabProperties> TabStops => _tabStops;

    /// <summary>
    /// Repeating tab width used after the last explicit stop, or for every tab when
    /// <see cref="TabStops"/> is empty. 0 / NaN → 4 × <see cref="FlowDocument.FontSize"/> (WPF default).
    /// </summary>
    public double IncrementalTab { get; set; }

    /// <summary>
    /// DIP scale used by <see cref="FormattedText"/> so spacer widths match on-screen glyphs.
    /// </summary>
    public double PixelsPerDip { get; set; } = 1d;

    public TabFlowDocument()
    {
        PagePadding = new Thickness(0d);
        TextAlignment = TextAlignment.Left;
    }

    /// <summary>
    /// MediaInfo-style lines: <c>Label : Value</c>. Measures labels with the document typeface
    /// (including CJK fallback) and plants one Left tab so the values share a column.
    /// Wrapped values hang at that tab (first-line negative indent), like a Word hanging indent.
    /// </summary>
    public void LoadKeyedLines(string text, string separator = DefaultColumnSeparator)
    {
        if (separator == null)
            throw new ArgumentNullException(nameof(separator));

        Blocks.Clear();
        _tabStops.Clear();

        if (string.IsNullOrEmpty(text))
            return;

        var lines = SplitLines(text);
        var labels = new List<string>();
        foreach (var line in lines)
        {
            var sep = line.IndexOf(separator, StringComparison.Ordinal);
            if (sep > 0)
                labels.Add(line.Substring(0, sep).TrimEnd());
        }

        var tabLocation = 0d;
        foreach (var label in labels)
            tabLocation = Math.Max(tabLocation, MeasureWidth(label, FontWeights.Normal));

        // Gap before the aligned colon, then the stop itself.
        if (tabLocation > 0)
        {
            tabLocation += Math.Max(8d, FontSize * 0.5);
            _tabStops.Add(new TextTabProperties(TextTabAlignment.Left, tabLocation, 0, 0));
        }

        var added = 0;
        foreach (var line in lines)
        {
            var sep = line.IndexOf(separator, StringComparison.Ordinal);
            if (sep > 0)
            {
                var label = line.Substring(0, sep).TrimEnd();
                var value = line.Substring(sep + separator.Length);
                Blocks.Add(BuildTabbedParagraph(label + "\t: " + value, FontWeights.Normal, hangingTab: tabLocation, added == 0));
            }
            else
            {
                Blocks.Add(BuildHeaderParagraph(line, added == 0));
            }

            added++;
        }
    }

    /// <summary>
    /// Generic lines containing '\t', resolved against <see cref="TabStops"/> / incremental tabs.
    /// </summary>
    public void LoadTabbedText(string text)
    {
        Blocks.Clear();
        if (string.IsNullOrEmpty(text))
            return;

        var added = 0;
        foreach (var line in SplitLines(text))
        {
            if (line.IndexOf('\t') >= 0)
                Blocks.Add(BuildTabbedParagraph(line, FontWeights.Normal, hangingTab: FirstLeftTabOrZero(), added == 0));
            else
                Blocks.Add(BuildHeaderParagraph(line, added == 0));
            added++;
        }
    }

    private Paragraph BuildHeaderParagraph(string text, bool first)
    {
        return new Paragraph(new Run(text))
        {
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0d, first ? 0d : 12d, 0d, 4d),
        };
    }

    private Paragraph BuildTabbedParagraph(string line, FontWeight weight, double hangingTab, bool first)
    {
        var paragraph = new Paragraph
        {
            FontWeight = weight,
            Margin = new Thickness(0d, first ? 0d : 1d, 0d, 1d),
        };

        // Hang wrapped value lines at the first tab, same idea as a Word hanging indent.
        if (hangingTab > 0)
        {
            paragraph.Padding = new Thickness(hangingTab, 0d, 0d, 0d);
            paragraph.TextIndent = -hangingTab;
        }

        var parts = line.Split('\t');
        var x = 0d;
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0)
            {
                var following = parts[i];
                var nextTab = i + 1 < parts.Length
                    ? following.IndexOf('\t')
                    : -1;
                if (nextTab >= 0)
                    following = following.Substring(0, nextTab);

                var advance = GetTabAdvance(x, following, weight);
                if (advance > 0.5)
                {
                    paragraph.Inlines.Add(new InlineUIContainer(new TabSpacer { Advance = advance })
                    {
                        BaselineAlignment = BaselineAlignment.Center,
                    });
                    x += advance;
                }
            }

            var text = parts[i];
            if (text.Length > 0)
            {
                paragraph.Inlines.Add(new Run(text));
                x += MeasureWidth(text, weight);
            }
        }

        return paragraph;
    }

    /// <summary>
    /// Distance to the next tab from <paramref name="currentX"/>, matching WPF TextFormatter:
    /// first explicit stop with Location &gt; currentX, else the next multiple of IncrementalTab.
    /// Alignment (Left/Center/Right/Character) follows <see cref="TextTabProperties"/>.
    /// </summary>
    public double GetTabAdvance(double currentX, string followingText, FontWeight followingWeight)
    {
        var location = ResolveNextTabLocation(currentX, out var alignment);

        var followingWidth = 0d;
        if (alignment != TextTabAlignment.Left && !string.IsNullOrEmpty(followingText))
        {
            if (alignment == TextTabAlignment.Character)
            {
                var aligning = FindStopAlignmentChar(location);
                var index = aligning != 0 ? followingText.IndexOf((char)aligning) : -1;
                followingWidth = index >= 0
                    ? MeasureWidth(followingText.Substring(0, index), followingWeight)
                    : MeasureWidth(followingText, followingWeight);
            }
            else
            {
                followingWidth = MeasureWidth(followingText, followingWeight);
            }
        }

        double target;
        switch (alignment)
        {
            case TextTabAlignment.Center:
                target = location - followingWidth * 0.5;
                break;
            case TextTabAlignment.Right:
            case TextTabAlignment.Character:
                target = location - followingWidth;
                break;
            default:
                target = location;
                break;
        }

        return Math.Max(0d, target - currentX);
    }

    private double ResolveNextTabLocation(double currentX, out TextTabAlignment alignment)
    {
        alignment = TextTabAlignment.Left;
        const double epsilon = 0.5;

        if (_tabStops.Count > 0)
        {
            TextTabProperties match = null;
            foreach (var stop in _tabStops)
            {
                if (stop.Location > currentX + epsilon &&
                    (match == null || stop.Location < match.Location))
                    match = stop;
            }

            if (match != null)
            {
                alignment = match.Alignment;
                return match.Location;
            }
        }

        var incremental = IncrementalTab > 0 && !double.IsNaN(IncrementalTab)
            ? IncrementalTab
            : 4d * FontSize;
        if (incremental <= 0)
            incremental = 4d * 12d;

        var next = Math.Floor(currentX / incremental) * incremental + incremental;
        if (next <= currentX + epsilon)
            next += incremental;
        return next;
    }

    private int FindStopAlignmentChar(double location)
    {
        foreach (var stop in _tabStops)
        {
            if (Math.Abs(stop.Location - location) < 0.5)
                return stop.AligningCharacter;
        }

        return 0;
    }

    private double FirstLeftTabOrZero()
    {
        var location = 0d;
        foreach (var stop in _tabStops)
        {
            if (stop.Alignment == TextTabAlignment.Left &&
                (location <= 0 || stop.Location < location))
                location = stop.Location;
        }

        return location;
    }

    private double MeasureWidth(string text, FontWeight weight)
    {
        if (string.IsNullOrEmpty(text))
            return 0d;

        var typeface = new Typeface(FontFamily, FontStyle, weight, FontStretch);
        var dip = PixelsPerDip > 0 ? PixelsPerDip : 1d;
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection,
            typeface,
            FontSize,
            Brushes.Black,
            null,
            TextFormattingMode.Ideal,
            dip);
        return formatted.WidthIncludingTrailingWhitespace;
    }

    private static List<string> SplitLines(string text)
    {
        var raw = text.Replace("\r\n", "\n").Split('\n');
        var lines = new List<string>(raw.Length);
        foreach (var line in raw)
        {
            var trimmed = line.TrimEnd();
            if (trimmed.Length > 0)
                lines.Add(trimmed);
        }

        return lines;
    }
}

/// <summary>
/// Public so FlowDocument undo/clipboard XAML (XamlWriter) can round-trip the spacer.
/// Nested non-public types throw InvalidOperationException on Blocks.Clear / copy.
/// </summary>
public sealed class TabSpacer : FrameworkElement
{
    public TabSpacer()
    {
        Focusable = false;
        IsHitTestVisible = false;
        Height = 0.1;
    }

    public double Advance
    {
        get => Width;
        set => Width = Math.Max(0d, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsNaN(Width) ? 0d : Width;
        return new Size(width, 0.1);
    }
}
