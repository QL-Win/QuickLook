using System.IO;
using System.Windows;
using ICSharpCode.AvalonEdit.Highlighting;

namespace QuickLook.Plugin.TextViewer
{
    public class Plugin : IViewer
    {
        private TextViewerPanel _tvp;

        public int Priority => 0;

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            const long MAX_SIZE = 20 * 1024 * 1024;

            // if there is a possible highlighting scheme (by file extension), treat it as a plain text file
            if (HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(path)) != null)
                return new FileInfo(path).Length <= MAX_SIZE;

            // otherwise, read the first 512 bytes as string (StreamReader handles encoding automatically),
            // check whether they are all printable chars. 
            using (var sr = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                var buffer = new char[512];
                var len = sr.Read(buffer, 0, 512);

                for (var i = 0; i < len; i++)
                {
                    if (!char.IsControl(buffer[i])) continue;

                    if (buffer[i] != '\r' && buffer[i] != '\n' && buffer[i] != '\t')
                        return false;
                }

                return new FileInfo(path).Length <= MAX_SIZE;
            }
        }

        public void Prepare(string path, ViewContentContainer container)
        {
            container.PreferedSize = new Size {Width = 800, Height = 600};
        }

        public void View(string path, ViewContentContainer container)
        {
            _tvp = new TextViewerPanel(path);

            container.SetContent(_tvp);
            container.Title = $"{Path.GetFileName(path)}";
        }

        public void Dispose()
        {
        }
    }
}