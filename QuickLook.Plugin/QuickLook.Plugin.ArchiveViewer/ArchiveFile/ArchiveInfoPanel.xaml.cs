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

using PureSharpCompress.Archives;
using PureSharpCompress.Common;
using PureSharpCompress.Readers;
using QuickLook.Common.Annotations;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QuickLook.Plugin.ArchiveViewer.ArchiveFile;

public partial class ArchiveInfoPanel : UserControl, IDisposable, INotifyPropertyChanged
{
    // ReaderFactory is slow... so limit its usage
    private static readonly string[] _useReader = [".tar.gz", ".tgz", ".tar.bz2", ".tar.lz", ".tar.xz"];

    private static readonly string _previewRoot = Path.Combine(Path.GetTempPath(), "QuickLook", "ArchivePreview");

    private readonly Dictionary<string, ArchiveFileEntry> _fileEntries = [];
    private readonly string _path;

    /// <summary>
    /// Folder inside the archive to show ("docs\sub"), or empty for the whole archive
    /// </summary>
    private readonly string _innerFolder = string.Empty;

    private bool _disposed;
    private bool _extracting;
    private double _loadPercent;
    private ulong _totalZippedSize;
    private string _type;

    public ArchiveInfoPanel(string path)
    {
        InitializeComponent();

        // design-time only
        Resources.MergedDictionaries.Clear();

        // a folder inside an archive comes as "C:\dir\file.zip\docs\sub": show the archive from that folder
        if (!File.Exists(path) && SplitArchivePath(path) is { } split)
        {
            path = split.Archive;
            _innerFolder = split.InnerFolder;
        }

        _path = path;
        fileListView.PreviewRequested += BeginPreviewEntry;

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

            // the shown root: the whole archive, or the folder inside it that was asked for
            var rootKey = FindEntryKey(_innerFolder);
            if (rootKey == null)
            {
                Dispatcher.Invoke(() => { lblLoading.Content = $"Folder not found in the archive: {_innerFolder}"; });
                return;
            }

            var folders = -1; // do not count root node
            var files = 0;
            ulong sizeU = 0L;
            _fileEntries.Where(e => IsUnder(e.Key, rootKey)).ForEach(e =>
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

                fileListView.SetDataContext(_fileEntries[rootKey].Children.Keys);
                archiveCount.Content = string.IsNullOrEmpty(rootKey)
                    ? $"{_type} archive{t}"
                    : $"Folder in {_type} archive {Path.GetFileName(_path)}{t}";
                archiveSizeC.Content =
                    $"Compressed size {((long)_totalZippedSize).ToPrettySize(2)}";
                archiveSizeU.Content = $"Uncompressed size {((long)sizeU).ToPrettySize(2)}";
            });

            LoadPercent = 100d;
        }).Start();
    }

    private void LoadItemsFromArchive(string path)
    {
        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        if (UseReader(path))
        {
            var reader = ReaderFactory.Open(fileStream, new ChardetReaderOptions());

            _type = reader.ArchiveType.ToString();

            while (reader.MoveToNextEntry())
            {
                if (_disposed)
                    return;
                LoadPercent = 100d * fileStream.Position / fileStream.Length;
                ProcessByLevel(reader.Entry);
            }
        }
        else
        {
            var archive = ArchiveFactory.Open(fileStream, new ChardetReaderOptions());

            _type = archive.Type.ToString();

            foreach (var entry in archive.Entries)
            {
                if (_disposed)
                    return;
                LoadPercent = 100d * fileStream.Position / fileStream.Length;
                ProcessByLevel(entry);
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
                Key = entry.Key,
                Encrypted = entry.IsEncrypted,
                Size = (ulong)entry.Size,
                ModifiedDate = entry.LastModifiedTime ?? new DateTime()
            });
        }
    }

    private static bool UseReader(string path)
    {
        return _useReader.Any(path.ToLower().EndsWith);
    }

    /// <summary>
    /// Extracts the entry to a temporary folder and previews it in QuickLook
    /// </summary>
    private void BeginPreviewEntry(ArchiveFileEntry entry)
    {
        if (_extracting || entry.Encrypted || string.IsNullOrEmpty(entry.Key))
            return;

        _extracting = true;
        lblLoading.Content = $"Extracting {entry.Name} ...";
        LoadPercent = 0d;

        Task.Run(() =>
        {
            string target = null;
            try
            {
                target = ExtractEntry(entry);
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
            }

            Dispatcher.Invoke(() =>
            {
                _extracting = false;
                LoadPercent = 100d;

                if (_disposed || target == null)
                    return;

                PluginHelper.InvokePreview(target);
            });
        });
    }

    private string ExtractEntry(ArchiveFileEntry entry)
    {
        CleanupPreviewFolders();

        var dir = Path.Combine(_previewRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, entry.Name);

        using var fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        if (UseReader(_path))
        {
            var reader = ReaderFactory.Open(fileStream, new ChardetReaderOptions());

            while (reader.MoveToNextEntry())
            {
                if (_disposed)
                    return null;
                if (reader.Entry.Key != entry.Key)
                    continue;

                using var source = reader.OpenEntryStream();
                WriteEntry(source, target, (ulong)reader.Entry.Size);
                return target;
            }
        }
        else
        {
            using var archive = ArchiveFactory.Open(fileStream, new ChardetReaderOptions());

            var archiveEntry = archive.Entries.FirstOrDefault(e => e.Key == entry.Key);
            if (archiveEntry != null)
            {
                using var source = archiveEntry.OpenEntryStream();
                WriteEntry(source, target, (ulong)archiveEntry.Size);
                return target;
            }
        }

        return null;
    }

    private void WriteEntry(Stream source, string target, ulong size)
    {
        using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.Read);

        var buffer = new byte[81920];
        ulong written = 0;
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (_disposed)
                throw new OperationCanceledException();

            output.Write(buffer, 0, read);
            written += (ulong)read;
            if (size > 0)
                LoadPercent = Math.Min(99d, 100d * written / size);
        }
    }

    /// <summary>
    /// Removes files extracted by previous previews. Files still in use (e.g. by a pinned window) are kept.
    /// </summary>
    private static void CleanupPreviewFolders()
    {
        if (!Directory.Exists(_previewRoot))
            return;

        foreach (var dir in Directory.GetDirectories(_previewRoot))
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
                // still in use
            }
        }
    }

    /// <summary>
    /// Splits "C:\dir\file.zip\docs\sub" into the archive file and the folder inside it ("docs\sub").
    /// </summary>
    internal static (string Archive, string InnerFolder)? SplitArchivePath(string path)
    {
        for (var parent = Path.GetDirectoryName(path); !string.IsNullOrEmpty(parent); parent = Path.GetDirectoryName(parent))
        {
            if (File.Exists(parent))
                return (parent, path.Substring(parent.Length).Trim('\\'));
            if (Directory.Exists(parent))
                return null;
        }

        return null;
    }

    /// <summary>
    /// The key of the folder entry named <paramref name="folder"/>, matched ignoring case (the shell may
    /// show names differently), or null when the archive has no such folder.
    /// </summary>
    private string FindEntryKey(string folder)
    {
        if (string.IsNullOrEmpty(folder))
            return string.Empty;
        if (_fileEntries.TryGetValue(folder, out var exact) && exact.IsFolder)
            return folder;

        return _fileEntries.FirstOrDefault(e => e.Value.IsFolder
            && string.Equals(e.Key, folder, StringComparison.OrdinalIgnoreCase)).Key;
    }

    private static bool IsUnder(string key, string rootKey)
    {
        return string.IsNullOrEmpty(rootKey)
            || key == rootKey
            || key.StartsWith(rootKey + "\\", StringComparison.Ordinal);
    }

    private string GetDirectoryName(string path)
    {
        var d = Path.GetDirectoryName(path);

        return d ?? string.Empty;
    }

    private string[] GetPathFragments(string path)
    {
        if (string.IsNullOrEmpty(path))
            return [];

        var frags = path.Split('\\', '/').Where(f => !string.IsNullOrEmpty(f)).ToArray();

        return [.. frags.Select((s, i) => frags.Take(i + 1).Aggregate((a, b) => a + "\\" + b))];
    }

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
