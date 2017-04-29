using System;
using System.IO;
using System.Windows;
using SevenZip;

namespace QuickLook.Plugin.ArchiveViewer
{
    public class Plugin : IViewer
    {
        private ArchiveInfoPanel _panel;

        public int Priority => 0;

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            try
            {
                using (var archive = new SevenZipExtractor(path))
                {
                    // dummy access to the data. If it throws exception, return false
                    if (archive.ArchiveFileData == null)
                        return false;

                    // ignore some formats
                    switch (archive.Format)
                    {
                        case InArchiveFormat.Chm:
                        case InArchiveFormat.Flv:
                        case InArchiveFormat.Elf:
                        case InArchiveFormat.Msi:
                        case InArchiveFormat.PE:
                        case InArchiveFormat.Swf:
                            return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public void Prepare(string path, ViewContentContainer container)
        {
            container.PreferedSize = new Size {Width = 800, Height = 600};
        }

        public void View(string path, ViewContentContainer container)
        {
            _panel = new ArchiveInfoPanel(path);

            container.SetContent(_panel);
            container.Title = $"{Path.GetFileName(path)}";
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            _panel?.Dispose();
        }

        ~Plugin()
        {
            Dispose();
        }
    }
}