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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace QuickLook.Plugin.ArchiveViewer.ArchiveFile;

/// <summary>
/// Presents an archive hierarchy with one virtualized items host instead of one
/// nested TreeViewItem host per directory. The data model is already constructed
/// off the UI thread by ArchiveInfoPanel.
/// </summary>
public partial class ArchiveFileListView : UserControl, IDisposable
{
    private readonly BatchObservableCollection<ArchiveFileRow> _rows = [];
    private readonly Dictionary<ArchiveFileEntry, ArchiveFileRow> _rowCache = [];
    private bool _disposed;

    public ArchiveFileListView()
    {
        InitializeComponent();
        fileList.ExpansionRequested = SetExpanded;
    }

    // Exposed to the ListBox via ElementName binding (no ambient DataContext required).
    public ObservableCollection<ArchiveFileRow> Rows => _rows;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        fileList.ExpansionRequested = null;
        fileList.ResetAutomationPeers();
        _rowCache.Clear();
        _rows.ReplaceAll([]);
        IconManager.ClearCache();
        GC.SuppressFinalize(this);
    }

    public void SetDataContext(object context)
    {
        if (_disposed)
            return;

        _rowCache.Clear();
        var entries = context as IEnumerable<ArchiveFileEntry>;
        fileList.ResetAutomationPeers();
        if (entries == null)
        {
            _rows.ReplaceAll([]);
            return;
        }

        var roots = entries.Select(entry => GetRow(entry, 0)).ToList();

        // Preserve the original control's behavior of expanding one root item.
        if (roots.Count == 1 && roots[0].HasChildren)
            roots[0].IsExpanded = true;

        var visible = new List<ArchiveFileRow>();
        foreach (var root in roots)
        {
            visible.Add(root);
            if (root.IsExpanded)
                visible.AddRange(EnumerateVisibleChildren(root));
        }

        _rows.ReplaceAll(visible);
    }

    private ArchiveFileRow GetRow(ArchiveFileEntry entry, int depth)
    {
        if (!_rowCache.TryGetValue(entry, out var row))
        {
            row = new ArchiveFileRow(entry, depth);
            _rowCache.Add(entry, row);
        }

        return row;
    }

    private IEnumerable<ArchiveFileRow> EnumerateVisibleChildren(ArchiveFileRow parent)
    {
        foreach (var child in parent.Entry.Children.Keys)
        {
            var row = GetRow(child, parent.Depth + 1);
            yield return row;

            // Keep expanded descendants when their ancestor is collapsed/re-expanded.
            if (row.IsExpanded)
            {
                foreach (var descendant in EnumerateVisibleChildren(row))
                    yield return descendant;
            }
        }
    }

    private void ToggleFolder(object sender, RoutedEventArgs e)
    {
        if (!(sender is Button button) || !(button.DataContext is ArchiveFileRow row))
            return;

        SetExpanded(row, !row.IsExpanded);
        SelectRow(row, true);
    }

    private void SetExpanded(ArchiveFileRow row, bool expanded)
    {
        if (_disposed || !row.HasChildren || row.IsExpanded == expanded)
            return;

        var index = _rows.IndexOf(row);
        if (index < 0)
            return;

        // Reset invalidates ListBox selection. Preserve it unless collapsing its ancestor.
        var selected = fileList.SelectedItem as ArchiveFileRow;
        var restoreFocus = fileList.IsKeyboardFocusWithin;
        if (row.IsExpanded)
        {
            // All descendants occupy a contiguous range in the flattened view.
            var end = index + 1;
            while (end < _rows.Count && _rows[end].Depth > row.Depth)
                end++;

            row.IsExpanded = false;
            if (selected != null && _rows.IndexOf(selected) > index && _rows.IndexOf(selected) < end)
                selected = row;
            _rows.RemoveRange(index + 1, end - index - 1);
        }
        else
        {
            var children = EnumerateVisibleChildren(row).ToList();
            row.IsExpanded = true;
            _rows.InsertRange(index + 1, children);
        }

        // A Reset avoids thousands of change notifications; keep the clicked row
        // reachable if the virtualizing panel rebuilt its realized item containers.
        SelectRow(selected ?? row, restoreFocus);
        fileList.RaiseExpansionChanged(row, !expanded);
    }

    private void SelectRow(ArchiveFileRow row, bool restoreFocus = false)
    {
        fileList.SelectedItem = row;
        fileList.ScrollIntoView(row);
        // A recycled container can disappear during Reset. Restore focus only if
        // the user was already navigating here; automation must not steal focus.
        if (restoreFocus || fileList.IsKeyboardFocusWithin)
        {
            fileList.UpdateLayout();
            (fileList.ItemContainerGenerator.ContainerFromItem(row) as ListBoxItem)?.Focus();
        }
    }

    private void SynchronizeHeaders(object sender, ScrollChangedEventArgs e)
    {
        if (e.HorizontalChange != 0)
            columnHeaders.ScrollToHorizontalOffset(e.HorizontalOffset);
    }

    private void ToggleRowByDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_disposed || e.ChangedButton != MouseButton.Left)
            return;

        var source = e.OriginalSource as DependencyObject;
        while (source != null && source != fileList)
        {
            // The expander already handles each click; do not toggle a third time.
            if (source is Button)
                return;
            if (source is ListBoxItem item && item.Content is ArchiveFileRow row)
            {
                SetExpanded(row, !row.IsExpanded);
                SelectRow(row, true);
                e.Handled = true;
                return;
            }

            source = source is FrameworkContentElement content ? content.Parent :
                source is Visual ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
        }
    }

    private void NavigateTree(object sender, KeyEventArgs e)
    {
        if (_disposed || Keyboard.Modifiers != ModifierKeys.None || _rows.Count == 0)
            return;

        var key = e.Key;
        if (FlowDirection == FlowDirection.RightToLeft)
        {
            if (key == Key.Left) key = Key.Right;
            else if (key == Key.Right) key = Key.Left;
        }

        var row = fileList.SelectedItem as ArchiveFileRow;
        var index = row == null ? -1 : _rows.IndexOf(row);
        switch (key)
        {
            case Key.Down:
                SelectRow(_rows[Math.Min(index + 1, _rows.Count - 1)]);
                break;
            case Key.Up:
                SelectRow(_rows[Math.Max(index - 1, 0)]);
                break;
            case Key.Home:
                SelectRow(_rows[0]);
                break;
            case Key.End:
                SelectRow(_rows[_rows.Count - 1]);
                break;
            case Key.Right:
            case Key.Add:
                if (row == null) SelectRow(_rows[0]);
                else if (!row.IsExpanded) SetExpanded(row, true);
                else if (key == Key.Right && index + 1 < _rows.Count && _rows[index + 1].Depth > row.Depth)
                    SelectRow(_rows[index + 1]);
                break;
            case Key.Left:
            case Key.Subtract:
                if (row == null) SelectRow(_rows[0]);
                else if (row.IsExpanded) SetExpanded(row, false);
                else if (key == Key.Left)
                    for (var i = index - 1; i >= 0; i--)
                        if (_rows[i].Depth < row.Depth)
                        {
                            SelectRow(_rows[i]);
                            break;
                        }
                break;
            default:
                return;
        }
        e.Handled = true;
    }
}

/// <summary>
/// Expansion state belongs to the data item, not the recycled WPF container.
/// </summary>
public sealed class ArchiveFileRow : INotifyPropertyChanged
{
    private bool _isExpanded;

    public ArchiveFileRow(ArchiveFileEntry entry, int depth)
    {
        Entry = entry;
        Depth = depth;
    }

    public ArchiveFileEntry Entry { get; }
    public int Depth { get; }
    public double IndentWidth => Math.Min(Depth * 16.0, 1024.0);
    public bool HasChildren => Entry.IsFolder && Entry.Children.Count != 0;
    public string ExpansionGlyph => !HasChildren ? string.Empty : IsExpanded ? "▾" : "▸";

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;

            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExpansionGlyph)));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}

/// <summary>
/// Large expansions produce one collection Reset instead of N UI notifications.
/// The underlying items remain available for ListBox's container virtualization.
/// </summary>
internal sealed class BatchObservableCollection<T> : ObservableCollection<T>
{
    public void ReplaceAll(IEnumerable<T> items)
    {
        Items.Clear();
        if (Items is List<T> list)
            list.AddRange(items);
        else
            foreach (var item in items)
                Items.Add(item);
        NotifyReset();
    }

    public void InsertRange(int index, IEnumerable<T> items)
    {
        var batch = items as IList<T> ?? items.ToList();
        if (batch.Count == 0)
            return;

        if (Items is List<T> list)
            list.InsertRange(index, batch);
        else
            foreach (var item in batch)
                Items.Insert(index++, item);
        NotifyReset();
    }

    public void RemoveRange(int index, int count)
    {
        if (count == 0)
            return;

        if (Items is List<T> list)
            list.RemoveRange(index, count);
        else
            for (var i = 0; i < count; i++)
                Items.RemoveAt(index);
        NotifyReset();
    }

    private void NotifyReset()
    {
        OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
