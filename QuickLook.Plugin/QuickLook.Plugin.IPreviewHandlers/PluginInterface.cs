using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace QuickLook.Plugin.IPreviewHandlers
{
    public class PluginInterface : IViewer
    {
        private PreviewPanel _panel;

        public int Priority => int.MaxValue;
        public bool AllowsTransparency => false;

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            switch (Path.GetExtension(path).ToLower())
            {
                case ".doc":
                case ".docx":
                case ".xls":
                case ".xlsx":
                case ".bbb":
                case ".xlsm":
                // Visio Viewer will not quit after preview, which cause serious memory issue
                //case ".vsd":
                //case ".vsdx":
                case ".ppt":
                case ".pptx":
                    return PreviewHandlerHost.GetPreviewHandlerGUID(path) != Guid.Empty;
            }

            return false;
        }

        public void Prepare(string path, ContextObject context)
        {
            context.SetPreferredSizeFit(new Size {Width = 800, Height = 800}, 0.8);
        }

        public void View(string path, ContextObject context)
        {
            _panel = new PreviewPanel();
            context.ViewerContent = _panel;
            context.Title = Path.GetFileName(path);

            _panel.PreviewFile(path, context);

            context.IsBusy = false;
        }

        public void Cleanup()
        {
            GC.SuppressFinalize(this);

            _panel?.Dispose();
            _panel = null;
        }
        
        ~PluginInterface()
        {
            Cleanup();
        }
    }
}