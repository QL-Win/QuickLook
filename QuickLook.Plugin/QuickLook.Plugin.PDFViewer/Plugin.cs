using System;
using System.IO;
using System.Text;

namespace QuickLook.Plugin.PDFViewer
{
    public class Plugin : IViewer
    {
        private PdfViewerControl _pdfControl;

        public int Priority => int.MaxValue;
        public bool AllowsTransparency => true;

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            if (File.Exists(path) && Path.GetExtension(path).ToLower() == ".pdf")
                return true;

            using (var br = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                return Encoding.ASCII.GetString(br.ReadBytes(4)) == "%PDF";
            }
        }

        public void Prepare(string path, ContextObject context)
        {
            _pdfControl = new PdfViewerControl();

            var desiredSize = _pdfControl.GetDesiredControlSizeByFirstPage(path);

            context.SetPreferredSizeFit(desiredSize, 0.8);
        }

        public void View(string path, ContextObject context)
        {
            context.ViewerContent = _pdfControl;

            _pdfControl.Loaded += (sender, e) =>
            {
                _pdfControl.LoadPdf(path);

                context.Title = $"{Path.GetFileName(path)} (1 / {_pdfControl.TotalPages})";
                _pdfControl.CurrentPageChanged += (sender2, e2) => context.Title =
                    $"{Path.GetFileName(path)} ({_pdfControl.CurrentPage + 1} / {_pdfControl.TotalPages})";

                context.IsBusy = false;
            };
        }

        public void Cleanup()
        {
            GC.SuppressFinalize(this);

            _pdfControl?.Dispose();
            _pdfControl = null;
        }

        ~Plugin()
        {
            Cleanup();
        }
    }
}