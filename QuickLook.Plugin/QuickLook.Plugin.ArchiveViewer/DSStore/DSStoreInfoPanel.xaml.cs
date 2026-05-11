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
using QuickLook.Plugin.ArchiveViewer.ArchiveFile;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QuickLook.Plugin.ArchiveViewer.DSStore;

public partial class DSStoreInfoPanel : UserControl, IDisposable, INotifyPropertyChanged
{
    private bool _disposed;
    private double _loadPercent;

    public DSStoreInfoPanel(string path)
    {
        InitializeComponent();

        // design-time only
        Resources.MergedDictionaries.Clear();

        BeginLoadArchive(path);
    }

    public double LoadPercent
    {
        get => _loadPercent;
        private set
        {
            if (value == _loadPercent) return;
            _loadPercent = value;
            OnPropertyChanged();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _disposed = true;
        fileListView.Dispose();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void BeginLoadArchive(string path)
    {
        new Task(() =>
        {
            var root = new ArchiveFileEntry(System.IO.Path.GetFileName(path), true);

            try
            {
                LoadItemsFromDSStore(path, root);
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
                Dispatcher.Invoke(() => { lblLoading.Content = "Preview failed. See log for more details."; });
                return;
            }

            var files = 0;
            foreach (var child in root.Children.Keys)
            {
                if (!child.IsFolder)
                    files++;
            }

            var f = files != 0 ? $"{files} files" : "0 files";

            Dispatcher.Invoke(() =>
            {
                if (_disposed) return;

                fileListView.SetDataContext(root.Children.Keys);
                archiveCount.Content = $"DS_Store, {f}";
                archiveSizeC.Content = string.Empty;
                archiveSizeU.Content = string.Empty;
            });

            LoadPercent = 100d;
        }).Start();
    }

    private void LoadItemsFromDSStore(string path, ArchiveFileEntry root)
    {
        List<string> fileNames = DSStoreExtractor.GetFileNames(path);

        // Deduplicate while preserving order
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in fileNames)
        {
            if (_disposed) return;
            if (string.IsNullOrEmpty(name)) continue;
            if (!seen.Add(name)) continue;

            _ = new ArchiveFileEntry(name, false, root);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
