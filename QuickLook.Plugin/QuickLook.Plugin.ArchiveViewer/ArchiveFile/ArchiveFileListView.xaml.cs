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
    }

    // Exposed to the ListBox via ElementName binding (no ambient DataContext required).
    public ObservableCollection<ArchiveFileRow> Rows => _rows;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
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
        if (_disposed || !(sender is Button button) || !(button.DataContext is ArchiveFileRow row)
            || !row.HasChildren)
            return;

        var index = _rows.IndexOf(row);
        if (index < 0)
            return;

        if (row.IsExpanded)
        {
            // All descendants occupy a contiguous range in the flattened view.
            var end = index + 1;
            while (end < _rows.Count && _rows[end].Depth > row.Depth)
                end++;

            row.IsExpanded = false;
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
        fileList.ScrollIntoView(row);
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
