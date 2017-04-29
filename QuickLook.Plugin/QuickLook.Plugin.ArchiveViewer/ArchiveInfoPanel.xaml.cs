using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using SevenZip;

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
            fileListView.Dispose();
        }

        private void LoadArchive(string path)
        {
            LoadItemsFromArchive(path);

            var folder = 0;
            var files = 0;
            ulong sizeU = 0L;
            _fileEntries.ForEach(e =>
            {
                if (e.Value.IsFolder)
                    folder++;
                else
                    files++;

                sizeU += e.Value.Size;
            });

            var s = _solid ? "solid" : "not solid";

            archiveCount.Content =
                $"{_type} archive, {s}, {folder - 1} folders and {files} files"; // do not count root node
            archiveSizeC.Content = $"Compressed size {_totalZippedSize.ToPrettySize(2)}";
            archiveSizeU.Content = $"Uncompressed size {sizeU.ToPrettySize(2)}";
        }

        private void LoadItemsFromArchive(string path)
        {
            using (var reader = new SevenZipExtractor(path))
            {
                _totalZippedSize = (ulong) reader.PackedSize;
                _solid = reader.IsSolid;
                _type = reader.Format.ToString();

                var root = new ArchiveFileEntry(Path.GetFileName(path), true);
                _fileEntries.Add("", root);

                foreach (var entry in reader.ArchiveFileData)
                    ProcessByLevel(entry);
            }
        }

        private void ProcessByLevel(ArchiveFileInfo entry)
        {
            var pf = GetPathFragments(entry.FileName);

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

                _fileEntries.Add(file, new ArchiveFileEntry(Path.GetFileName(entry.FileName), false, parent)
                {
                    Encrypted = entry.Encrypted,
                    Size = entry.Size,
                    ModifiedDate = entry.LastWriteTime
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
            var frags = path.Split('\\');

            return frags.Select((s, i) => frags.Take(i + 1).Aggregate((a, b) => a + "\\" + b)).ToArray();
        }
    }
}