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
    public CsvViewerPanel()
    {
        InitializeComponent();

        searchPanel.Visibility = Visibility.Collapsed;
        searchPanel.SearchTextChanged += SearchPanel_SearchTextChanged;
        searchPanel.MatchCaseChanged += SearchPanel_MatchCaseChanged;
        searchPanel.FindNextRequested += SearchPanel_FindNextRequested;
        searchPanel.FindPreviousRequested += SearchPanel_FindPreviousRequested;
        searchPanel.CloseRequested += SearchPanel_CloseRequested;

        PreviewKeyDown += CsvViewerPanel_PreviewKeyDown;
        dataGrid.LoadingRow += DataGrid_LoadingRow;
    }

    public List<string[]> Rows { get; private set; } = [];

    private readonly List<(int RowIndex, int ColumnIndex)> _matches = new();
    private readonly HashSet<(int RowIndex, int ColumnIndex)> _matchSet = new();
    private int _currentMatchIndex = -1;

    public void LoadFile(string path)
    {
        const int limit = 10000;
        var binded = false;

        Rows.Clear();
        dataGrid.Columns.Clear();
        _matches.Clear();
        _matchSet.Clear();
        _currentMatchIndex = -1;
        searchPanel.SetMatchCount(0, 0);
        searchPanel.Visibility = Visibility.Collapsed;

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

    private void SearchPanel_SearchTextChanged(object sender, string searchText)
    {
        ExecuteSearch();
    }

    private void SearchPanel_MatchCaseChanged(object sender, bool matchCase)
    {
        ExecuteSearch();
    }

    private void SearchPanel_FindNextRequested(object sender, EventArgs e)
    {
        MoveMatch(1);
    }

    private void SearchPanel_FindPreviousRequested(object sender, EventArgs e)
    {
        MoveMatch(-1);
    }

    private void SearchPanel_CloseRequested(object sender, EventArgs e)
    {
        HideSearchPanel();
    }

    private void CsvViewerPanel_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            ShowSearchPanel();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F3)
        {
            MoveMatch(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && searchPanel.Visibility == Visibility.Visible)
        {
            HideSearchPanel();
            e.Handled = true;
        }
    }

    private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        UpdateRowHighlights(e.Row);
    }

    private void UpdateRowHighlights(DataGridRow row)
    {
        if (row.Item is not string[] rowData)
        {
            return;
        }

        var rowIndex = dataGrid.ItemContainerGenerator.IndexFromContainer(row);
        for (var columnIndex = 0; columnIndex < dataGrid.Columns.Count; columnIndex++)
        {
            var cell = GetCell(row, columnIndex);
            if (cell == null)
            {
                continue;
            }

            if (_matchSet.Contains((rowIndex, columnIndex)))
            {
                cell.Background = _currentMatchIndex >= 0 && _matches[_currentMatchIndex].RowIndex == rowIndex && _matches[_currentMatchIndex].ColumnIndex == columnIndex
                    ? CurrentMatchBrush
                    : MatchBrush;
            }
            else
            {
                cell.ClearValue(BackgroundProperty);
            }
        }
    }

    private void ExecuteSearch()
    {
        var query = searchPanel.SearchText ?? string.Empty;
        _matches.Clear();
        _matchSet.Clear();
        _currentMatchIndex = -1;

        if (!string.IsNullOrEmpty(query))
        {
            var comparison = searchPanel.MatchCase
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            for (var rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
            {
                var row = Rows[rowIndex];
                for (var columnIndex = 0; columnIndex < row.Length; columnIndex++)
                {
                    var cellValue = row[columnIndex];
                    if (!string.IsNullOrEmpty(cellValue) && cellValue.IndexOf(query, comparison) >= 0)
                    {
                        _matches.Add((rowIndex, columnIndex));
                        _matchSet.Add((rowIndex, columnIndex));
                    }
                }
            }

            if (_matches.Count > 0)
            {
                _currentMatchIndex = 0;
            }
        }

        searchPanel.SetMatchCount(_currentMatchIndex + 1, _matches.Count);
        UpdateVisibleCellHighlights();
        UpdateCurrentMatchSelection();
    }

    private void MoveMatch(int direction)
    {
        if (_matches.Count == 0)
        {
            return;
        }

        _currentMatchIndex = (_currentMatchIndex + direction + _matches.Count) % _matches.Count;
        UpdateCurrentMatchSelection();
    }

    private void UpdateCurrentMatchSelection()
    {
        if (_matches.Count == 0)
        {
            dataGrid.SelectedCells.Clear();
            return;
        }

        var match = _matches[_currentMatchIndex];
        if (match.RowIndex < 0 || match.RowIndex >= Rows.Count)
        {
            return;
        }

        var rowItem = Rows[match.RowIndex];
        if (match.ColumnIndex < 0 || match.ColumnIndex >= dataGrid.Columns.Count)
        {
            return;
        }

        var column = dataGrid.Columns[match.ColumnIndex];
        dataGrid.SelectedCells.Clear();
        var cellInfo = new DataGridCellInfo(rowItem, column);
        dataGrid.CurrentCell = cellInfo;
        dataGrid.SelectedCells.Add(cellInfo);
        dataGrid.ScrollIntoView(rowItem, column);

        Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdateVisibleCellHighlights();

            var row = GetRow(match.RowIndex);
            var cell = GetCell(row, match.ColumnIndex);
            cell?.Focus();
        }), System.Windows.Threading.DispatcherPriority.Background);

        searchPanel.SetMatchCount(_currentMatchIndex + 1, _matches.Count);
    }

    private void ShowSearchPanel()
    {
        searchPanel.Visibility = Visibility.Visible;
        searchPanel.FocusSearchText();
    }

    private void HideSearchPanel()
    {
        searchPanel.Visibility = Visibility.Collapsed;
        dataGrid.SelectedCells.Clear();
        _matches.Clear();
        _matchSet.Clear();
        _currentMatchIndex = -1;
        searchPanel.SetMatchCount(0, 0);
        UpdateVisibleCellHighlights();
    }

    private void UpdateVisibleCellHighlights()
    {
        foreach (var row in GetVisibleRows())
        {
            if (row.Item is not string[] rowData)
            {
                continue;
            }

            var rowIndex = dataGrid.ItemContainerGenerator.IndexFromContainer(row);
            for (var columnIndex = 0; columnIndex < dataGrid.Columns.Count; columnIndex++)
            {
                var cell = GetCell(row, columnIndex);
                if (cell == null)
                {
                    continue;
                }

                if (_matchSet.Contains((rowIndex, columnIndex)))
                {
                    cell.Background = _currentMatchIndex >= 0 && _matches[_currentMatchIndex].RowIndex == rowIndex && _matches[_currentMatchIndex].ColumnIndex == columnIndex
                        ? CurrentMatchBrush
                        : MatchBrush;
                }
                else
                {
                    cell.ClearValue(BackgroundProperty);
                }
            }
        }
    }

    private IEnumerable<DataGridRow> GetVisibleRows()
    {
        for (var index = 0; index < dataGrid.Items.Count; index++)
        {
            var row = dataGrid.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
            if (row != null)
            {
                yield return row;
            }
        }
    }

    private DataGridRow GetRow(int index)
    {
        if (index < 0 || index >= dataGrid.Items.Count)
        {
            return null;
        }

        var row = dataGrid.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
        if (row != null)
        {
            return row;
        }

        dataGrid.UpdateLayout();
        dataGrid.ScrollIntoView(dataGrid.Items[index]);
        return dataGrid.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
    }

    private DataGridCell GetCell(DataGridRow row, int columnIndex)
    {
        if (row == null)
        {
            return null;
        }

        var presenter = FindVisualChild<DataGridCellsPresenter>(row);
        if (presenter == null)
        {
            row.ApplyTemplate();
            presenter = FindVisualChild<DataGridCellsPresenter>(row);
        }

        if (presenter == null)
        {
            return null;
        }

        var cell = presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
        if (cell == null)
        {
            dataGrid.ScrollIntoView(row.Item, dataGrid.Columns[columnIndex]);
            cell = presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
        }

        return cell;
    }

    private static T FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        if (parent == null)
        {
            return null;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }

            var foundChild = FindVisualChild<T>(child);
            if (foundChild != null)
            {
                return foundChild;
            }
        }

        return null;
    }

    private static readonly Brush MatchBrush = CreateFrozenBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0x00));
    private static readonly Brush CurrentMatchBrush = CreateFrozenBrush(Color.FromArgb(0xAA, 0xFF, 0xD3, 0x00));

    private static Brush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        if (brush.CanFreeze)
        {
            brush.Freeze();
        }

        return brush;
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
