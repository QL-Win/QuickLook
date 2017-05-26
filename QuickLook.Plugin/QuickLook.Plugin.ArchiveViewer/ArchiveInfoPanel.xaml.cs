using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using SharpCompress.Archives;

namespace QuickLook.Plugin.ArchiveViewer
{
    /// <summary>
    ///     Interaction logic for ArchiveInfoPanel.xaml
    /// </summary>
    public partial class ArchiveInfoPanel : UserControl, IDisposable
    {
        private readonly Dictionary<string, ArchiveFileEntry> _fileEntries = new Dictionary<string, ArchiveFileEntry>();
        private bool _solid;
        private ulong _totalZippedSize;
        private string _type;

        public ArchiveInfoPanel(string path)
        {
            InitializeComponent();

            LoadArchive(path);

            fileListView.SetDataContext(_fileEntries[""].Children.Keys);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            fileListView.Dispose();
        }

        ~ArchiveInfoPanel()
        {
            Dispose();
        }

        private void LoadArchive(string path)
        {
            LoadItemsFromArchive(path);

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

            var s = _solid ? ", solid" : "";

            string t;
            var d = folders != 0 ? $"{folders} folders" : string.Empty;
            var f = files != 0 ? $"{files} files" : string.Empty;
            if (!string.IsNullOrEmpty(d) && !string.IsNullOrEmpty(f))
                t = $", {d} and {f}";
            else if (string.IsNullOrEmpty(d) && string.IsNullOrEmpty(f))
                t = string.Empty;
            else
                t = $", {d}{f}";

            archiveCount.Content =
                $"{_type} archive{s}{t}";
            archiveSizeC.Content = $"Compressed size {_totalZippedSize.ToPrettySize(2)}";
            archiveSizeU.Content = $"Uncompressed size {sizeU.ToPrettySize(2)}";
        }

        private void LoadItemsFromArchive(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var archive = ArchiveFactory.Open(stream);

                _totalZippedSize = (ulong) archive.TotalSize;
                _solid = archive.IsSolid;
                _type = archive.Type.ToString();

                var root = new ArchiveFileEntry(Path.GetFileName(path), true);
                _fileEntries.Add("", root);

                foreach (var entry in archive.Entries)
                    ProcessByLevel(entry);
            }
        }

        private void ProcessByLevel(IArchiveEntry entry)
        {
            var pf = GetPathFragments(entry.Key);

            // process folders. When entry is a directory, all fragments are folders.
            pf.Take(entry.IsDirectory ? pf.Length : pf.Length - 1)
                .ForEach(f =>
                {
                    // skip if current dir is already added
                    if (_fileEntries.ContainsKey(f))
                        return;

                    ArchiveFileEntry parent;
                    _fileEntries.TryGetValue(GetDirectoryName(f), out parent);

                    var afe = new ArchiveFileEntry(Path.GetFileName(f), true, parent);

                    _fileEntries.Add(f, afe);
                });

            // add the last path fragments, which is a file
            if (!entry.IsDirectory)
            {
                var file = pf.Last();

                ArchiveFileEntry parent;
                _fileEntries.TryGetValue(GetDirectoryName(file), out parent);

                _fileEntries.Add(file, new ArchiveFileEntry(Path.GetFileName(entry.Key), false, parent)
                {
                    Encrypted = entry.IsEncrypted,
                    Size = (ulong) entry.Size,
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
    }
}