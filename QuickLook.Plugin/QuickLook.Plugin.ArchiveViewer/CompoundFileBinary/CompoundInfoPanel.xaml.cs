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

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using QuickLook.Plugin.ArchiveViewer.ArchiveFile;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QuickLook.Plugin.ArchiveViewer.CompoundFileBinary;

public partial class CompoundInfoPanel : UserControl, IDisposable, INotifyPropertyChanged
{
    private readonly Dictionary<string, ArchiveFileEntry> _fileEntries = [];
    private bool _disposed;
    private double _loadPercent;
    private ulong _totalSize;

    public CompoundInfoPanel(string path)
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
            _totalSize = (ulong)new FileInfo(path).Length;

            var root = new ArchiveFileEntry(Path.GetFileName(path), true);
            _fileEntries.Add(string.Empty, root);

            try
            {
                LoadItemsFromArchive(path);
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
                Dispatcher.Invoke(() => { lblLoading.Content = "Preview failed. See log for more details."; });
                return;
            }

            var folders = -1; // do not count root node
            var files = 0;
            ulong sizeU = 0L;

            foreach (var item in _fileEntries)
            {
                if (item.Value.IsFolder)
                    folders++;
                else
                    files++;

                sizeU += item.Value.Size;
            }

            string t;
            var d = folders != 0 ? $"{folders} folders" : string.Empty;
            var f = files != 0 ? $"{files} files" : string.Empty;
            if (!string.IsNullOrEmpty(d) && !string.IsNullOrEmpty(f))
                t = $", {d} and {f}";
            else if (string.IsNullOrEmpty(d) && string.IsNullOrEmpty(f))
                t = string.Empty;
            else
                t = $", {d}{f}";

            Dispatcher.Invoke(() =>
            {
                if (_disposed)
                    return;

                fileListView.SetDataContext(_fileEntries[string.Empty].Children.Keys);
                archiveCount.Content = $"Compound File{t}";
                archiveSizeC.Content = string.Empty;
                archiveSizeU.Content = $"Total stream size {((long)sizeU).ToPrettySize(2)}";
            });

            LoadPercent = 100d;
        }).Start();
    }

    private void LoadItemsFromArchive(string path)
    {
        using var storage = new DisposableIStorage(path, STGM.READ | STGM.SHARE_DENY_WRITE, IntPtr.Zero);
        ProcessStorage(storage, string.Empty);
    }

    private void ProcessStorage(DisposableIStorage storage, string currentPath)
    {
        var enumerator = storage.EnumElements();
        while (enumerator.MoveNext())
        {
            if (_disposed) return;

            var stat = enumerator.Current;
            var name = stat.pwcsName;
            var fullPath = string.IsNullOrEmpty(currentPath) ? name : currentPath + "\\" + name;

            _fileEntries.TryGetValue(currentPath, out var parent);

            if (stat.type == (int)STGTY.STGTY_STORAGE)
            {
                var entry = new ArchiveFileEntry(name, true, parent);
                _fileEntries.Add(fullPath, entry);

                using var subStorage = storage.OpenStorage(name, null, STGM.READ | STGM.SHARE_EXCLUSIVE, IntPtr.Zero);
                ProcessStorage(subStorage, fullPath);
            }
            else if (stat.type == (int)STGTY.STGTY_STREAM)
            {
                long fileTime = ((long)stat.mtime.dwHighDateTime << 32) | (uint)stat.mtime.dwLowDateTime;
                var entry = new ArchiveFileEntry(name, false, parent)
                {
                    Size = (ulong)stat.cbSize,
                    ModifiedDate = DateTime.FromFileTimeUtc(fileTime).ToLocalTime()
                };
                _fileEntries.Add(fullPath, entry);
            }
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
