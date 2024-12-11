// Copyright © 2017 Paddy Xu
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

using QuickLook.Common.Annotations;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QuickLook.Plugin.ArchiveViewer;

/// <summary>
///     Interaction logic for ArchiveInfoPanel.xaml
/// </summary>
public partial class ArchiveInfoPanel : UserControl, IDisposable, INotifyPropertyChanged
{
    private readonly Dictionary<string, ArchiveFileEntry> _fileEntries = new Dictionary<string, ArchiveFileEntry>();
    private bool _disposed;
    private double _loadPercent;
    private ulong _totalZippedSize;
    private string _type;

    public ArchiveInfoPanel(string path)
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
            _totalZippedSize = (ulong)new FileInfo(path).Length;

            var root = new ArchiveFileEntry(Path.GetFileName(path), true);
            _fileEntries.Add("", root);

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
            _fileEntries.ForEach(e =>
            {
                if (e.Value.IsFolder)
                    folders++;
                else
                    files++;

                sizeU += e.Value.Size;
            });

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

                fileListView.SetDataContext(_fileEntries[""].Children.Keys);
                archiveCount.Content =
                    $"{_type} archive{t}";
                archiveSizeC.Content =
                    $"Compressed size {((long)_totalZippedSize).ToPrettySize(2)}";
                archiveSizeU.Content = $"Uncompressed size {((long)sizeU).ToPrettySize(2)}";
            });

            LoadPercent = 100d;
        }).Start();
    }

    private void LoadItemsFromArchive(string path)
    {
        using (var stream = File.OpenRead(path))
        {
            // ReaderFactory is slow... so limit its usage
            string[] useReader = { ".tar.gz", ".tgz", ".tar.bz2", ".tar.lz", ".tar.xz" };

            if (useReader.Any(path.ToLower().EndsWith))
            {
                var reader = ReaderFactory.Open(stream, new ChardetReaderOptions());

                _type = reader.ArchiveType.ToString();

                while (reader.MoveToNextEntry())
                {
                    if (_disposed)
                        return;
                    LoadPercent = 100d * stream.Position / stream.Length;
                    ProcessByLevel(reader.Entry);
                }
            }
            else
            {
                var archive = ArchiveFactory.Open(stream, new ChardetReaderOptions());

                _type = archive.Type.ToString();

                foreach (var entry in archive.Entries)
                {
                    if (_disposed)
                        return;
                    LoadPercent = 100d * stream.Position / stream.Length;
                    ProcessByLevel(entry);
                }
            }
        }
    }

    private void ProcessByLevel(IEntry entry)
    {
        var pf = GetPathFragments(entry.Key);

        // process folders. When entry is a directory, all fragments are folders.
        pf.Take(entry.IsDirectory ? pf.Length : pf.Length - 1)
            .ForEach(f =>
            {
                // skip if current dir is already added
                if (_fileEntries.ContainsKey(f))
                    return;

                _fileEntries.TryGetValue(GetDirectoryName(f), out var parent);

                var afe = new ArchiveFileEntry(Path.GetFileName(f), true, parent);

                _fileEntries.Add(f, afe);
            });

        // add the last path fragments, which is a file
        if (!entry.IsDirectory)
        {
            var file = pf.Last();

            _fileEntries.TryGetValue(GetDirectoryName(file), out var parent);

            _fileEntries.Add(file, new ArchiveFileEntry(Path.GetFileName(entry.Key), false, parent)
            {
                Encrypted = entry.IsEncrypted,
                Size = (ulong)entry.Size,
                ModifiedDate = entry.LastModifiedTime ?? new DateTime()
            });
        }
    }

    private string GetDirectoryName(string path)
    {
        var d = Path.GetDirectoryName(path);

        return d ?? "";
    }

    private string[] GetPathFragments(string path)
    {
        if (string.IsNullOrEmpty(path))
            return new string[0];

        var frags = path.Split('\\', '/').Where(f => !string.IsNullOrEmpty(f)).ToArray();

        return frags.Select((s, i) => frags.Take(i + 1).Aggregate((a, b) => a + "\\" + b)).ToArray();
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
