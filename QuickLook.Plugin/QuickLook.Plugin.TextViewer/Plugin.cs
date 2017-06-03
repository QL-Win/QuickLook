using System.IO;
using System.Windows;
using ICSharpCode.AvalonEdit.Highlighting;

namespace QuickLook.Plugin.TextViewer
{
    public class Plugin : IViewer
    {
        private TextViewerPanel _tvp;

        public int Priority => 0;
        public bool AllowsTransparency => true;

        public void Init()
        {
        }

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

        public void Prepare(string path, ContextObject context)
        {
            context.PreferredSize = new Size {Width = 800, Height = 600};
            context.CanFocus = true;
        }

        public void View(string path, ContextObject context)
        {
            _tvp = new TextViewerPanel(path);

            context.ViewerContent = _tvp;
            context.Title = $"{Path.GetFileName(path)}";

            context.IsBusy = false;
        }

        public void Cleanup()
        {
            _tvp = null;
        }
    }
}