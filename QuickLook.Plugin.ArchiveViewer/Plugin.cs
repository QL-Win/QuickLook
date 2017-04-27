using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace QuickLook.Plugin.ArchiveViewer
{
    public class Plugin : IViewer
    {
        public int Priority => 0;

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            using (var s = File.OpenRead(path))
            {
                // The 7Zip format doesn't allow for reading as a forward-only stream so 
                // 7Zip is only supported through the Archive API.
                if (SevenZipArchive.IsSevenZipFile(s))
                    return true;

                s.Seek(0, SeekOrigin.Begin);
                try
                {
                    ReaderFactory.Open(s);
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }

            return true;
        }

        public void Prepare(string path, ViewContentContainer container)
        {
            container.PreferedSize = new Size {Width = 800, Height = 600};
        }

        public void View(string path, ViewContentContainer container)
        {
            var files = new List<IEntry>();

            if (SevenZipArchive.IsSevenZipFile(path))
                GetItemsFromSevenZip(path, files);
            else
                GetItemsFromIReader(path, files);
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        private void GetItemsFromSevenZip(string path, List<IEntry> files)
        {
            using (var s = File.OpenRead(path))
            {
                using (var reader = SevenZipArchive.Open(s))
                {
                    foreach (var entry in reader.Entries)
                        files.Add(entry);
                }
            }
        }

        private void GetItemsFromIReader(string path, List<IEntry> files)
        {
            using (var s = File.OpenRead(path))
            {
                using (var reader = ReaderFactory.Open(s))
                {
                    while (reader.MoveToNextEntry())
                        files.Add(reader.Entry);
                }
            }
        }
    }
}