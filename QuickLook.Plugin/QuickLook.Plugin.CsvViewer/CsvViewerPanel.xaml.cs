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

using CsvHelper;
using CsvHelper.Configuration;
using QuickLook.Plugin.CsvViewer.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using UtfUnknown;

namespace QuickLook.Plugin.CsvViewer;

public partial class CsvViewerPanel : UserControl
{
    // Highlight color for search results (semi-transparent yellow)
    private static readonly SolidColorBrush HighlightBrush = new SolidColorBrush(Color.FromArgb(128, 255, 255, 0));

    private List<(int Row, int Column)> _searchResults = new List<(int, int)>();
    private int _currentResultIndex = -1;
    private string _currentSearchText = string.Empty;
    private bool _currentMatchCase;
    private DataGridCell _highlightedCell;  // Track currently highlighted cell for efficient clearing

    public CsvViewerPanel()
    {
        InitializeComponent();

        KeyDown += CsvViewerPanel_KeyDown;
        searchPanel.SearchRequested += SearchPanel_SearchRequested;
        searchPanel.NavigateRequested += SearchPanel_NavigateRequested;
        searchPanel.CloseRequested += SearchPanel_CloseRequested;
    }

    public List<string[]> Rows { get; private set; } = [];

    private void CsvViewerPanel_KeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.F)
        {
            OpenSearchPanel();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && searchPanel.Visibility == Visibility.Visible)
        {
            CloseSearchPanel();
            e.Handled = true;
        }
        else if (e.Key == Key.F3)
        {
            if (searchPanel.Visibility == Visibility.Visible && _searchResults.Count > 0)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    NavigateToPreviousResult();
                }
                else
                {
                    NavigateToNextResult();
                }
            }
            e.Handled = true;
        }
    }

    private void OpenSearchPanel()
    {
        searchPanel.Visibility = Visibility.Visible;
        searchPanel.Focus();
    }

    private void CloseSearchPanel()
    {
        searchPanel.Visibility = Visibility.Collapsed;
        ClearHighlighting();
        _searchResults.Clear();
        _currentResultIndex = -1;
        dataGrid.Focus();
    }

    private void SearchPanel_SearchRequested(object sender, SearchEventArgs e)
    {
        _currentSearchText = e.SearchText;
        _currentMatchCase = e.MatchCase;
        PerformSearch();
    }

    private void SearchPanel_NavigateRequested(object sender, NavigateEventArgs e)
    {
        if (e.Forward)
        {
            NavigateToNextResult();
        }
        else
        {
            NavigateToPreviousResult();
        }
    }

    private void SearchPanel_CloseRequested(object sender, EventArgs e)
    {
        CloseSearchPanel();
    }

    private void PerformSearch()
    {
        ClearHighlighting();
        _searchResults.Clear();
        _currentResultIndex = -1;

        if (string.IsNullOrEmpty(_currentSearchText))
        {
            searchPanel.UpdateMatchCount(0, _currentResultIndex);
            return;
        }

        var comparison = _currentMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        for (int rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
        {
            var row = Rows[rowIndex];
            for (int colIndex = 0; colIndex < row.Length; colIndex++)
            {
                if (row[colIndex] != null && row[colIndex].IndexOf(_currentSearchText, comparison) >= 0)
                {
                    _searchResults.Add((rowIndex, colIndex));
                }
            }
        }

        if (_searchResults.Count > 0)
        {
            _currentResultIndex = 0;
            NavigateToCurrentResult();
        }

        searchPanel.UpdateMatchCount(_searchResults.Count, _currentResultIndex);
    }

    private void NavigateToNextResult()
    {
        if (_searchResults.Count == 0)
            return;

        _currentResultIndex = (_currentResultIndex + 1) % _searchResults.Count;
        NavigateToCurrentResult();
        searchPanel.UpdateMatchCount(_searchResults.Count, _currentResultIndex);
    }

    private void NavigateToPreviousResult()
    {
        if (_searchResults.Count == 0)
            return;

        _currentResultIndex = (_currentResultIndex - 1 + _searchResults.Count) % _searchResults.Count;
        NavigateToCurrentResult();
        searchPanel.UpdateMatchCount(_searchResults.Count, _currentResultIndex);
    }

    private void NavigateToCurrentResult()
    {
        if (_currentResultIndex < 0 || _currentResultIndex >= _searchResults.Count)
            return;

        var (rowIndex, colIndex) = _searchResults[_currentResultIndex];

        // Scroll to the row
        if (rowIndex < dataGrid.Items.Count)
        {
            dataGrid.ScrollIntoView(dataGrid.Items[rowIndex]);
            dataGrid.UpdateLayout();

            // Select the cell
            dataGrid.SelectedIndex = rowIndex;

            // Try to highlight the specific cell
            HighlightCurrentCell(rowIndex, colIndex);
        }
    }

    private void HighlightCurrentCell(int rowIndex, int colIndex)
    {
        // Clear previous highlight first
        ClearHighlighting();

        try
        {
            var row = dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
            if (row != null)
            {
                var presenter = FindVisualChild<DataGridCellsPresenter>(row);
                if (presenter != null)
                {
                    var cell = presenter.ItemContainerGenerator.ContainerFromIndex(colIndex) as DataGridCell;
                    if (cell != null)
                    {
                        cell.Background = HighlightBrush;
                        _highlightedCell = cell;
                    }
                }
            }
        }
        catch (InvalidOperationException)
        {
            // Can occur when visual tree is being rebuilt during scrolling.
            // Safe to ignore as the cell will be highlighted on next navigation.
        }
    }

    private void ClearHighlighting()
    {
        // Only clear the previously highlighted cell instead of iterating all cells
        if (_highlightedCell != null)
        {
            _highlightedCell.ClearValue(DataGridCell.BackgroundProperty);
            _highlightedCell = null;
        }
    }

    private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }
            var result = FindVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    public void LoadFile(string path)
    {
        const int limit = 10000;
        var binded = false;

        var encoding = CharsetDetector.DetectFromFile(path).Detected?.Encoding ??
                       Encoding.Default;

        using var sr = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), encoding);
      
        // Use fixed delimiters for known extensions to avoid mis-detection on small samples.
        var extension = Path.GetExtension(path);
        var delimiter = extension.Equals(".tsv", StringComparison.OrdinalIgnoreCase)
            ? "\t"
            : extension.Equals(".psv", StringComparison.OrdinalIgnoreCase)
                ? "|"
                : null;

        var conf = new CsvConfiguration(CultureInfo.CurrentUICulture)
        {
            MissingFieldFound = null,
            BadDataFound = null,
            DetectDelimiter = delimiter == null,
        };

        if (delimiter != null)
        {
            // Force delimiter for TSV/PSV so CsvHelper doesn't auto-detect incorrectly.
            conf.Delimiter = delimiter;
        }

        using var parser = new CsvParser(sr, conf);
        var i = 0;
        while (parser.Read())
        {
            var row = parser.Record;
            if (row == null)
                break;
            row = Concat([$"{i++ + 1}".PadLeft(6)], row);

            if (!binded)
            {
                SetupColumnBinding(row.Length);
                binded = true;
            }

            if (i > limit)
            {
                Rows.Add([.. Enumerable.Repeat("...", row.Length)]);
                break;
            }

            Rows.Add(row);
        }
    }

    private void SetupColumnBinding(int rowLength)
    {
        for (var i = 0; i < rowLength; i++)
        {
            var col = new DataGridTextColumn
            {
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeight.FromOpenTypeWeight(i == 0 ? 700 : 400),
                Binding = new Binding($"[{i}]")
            };
            dataGrid.Columns.Add(col);
        }
    }

    public static T[] Concat<T>(T[] x, T[] y)
    {
        if (x == null) throw new ArgumentNullException("x");
        if (y == null) throw new ArgumentNullException("y");
        var oldLen = x.Length;
        Array.Resize(ref x, x.Length + y.Length);
        Array.Copy(y, 0, x, oldLen, y.Length);
        return x;
    }
}
