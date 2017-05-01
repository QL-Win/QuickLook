using System;
using System.IO;
using System.Windows;
using QuickLook.Plugin.PDFViewer;

namespace QuickLook.Plugin.OfficeViewer
{
    public class PluginInterface : IViewer
    {
        private string _pdfPath = "";
        private PdfViewerControl _pdfViewer;

        public int Priority => int.MaxValue;

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
                case ".ppt":
                case ".pptx":
                    return true;
            }

            return false;
        }

        public void BoundViewSize(string path, ContextObject context)
        {
            context.SetPreferredSizeFit(new Size {Width = 800, Height = 600}, 0.8);
        }

        public void View(string path, ContextObject context)
        {
            using (var officeApp = new OfficeInteropWrapper(path))
            {
                _pdfPath = officeApp.SaveAsPdf();
            }

            if (string.IsNullOrEmpty(_pdfPath))
                throw new Exception("COM failed.");

            _pdfViewer = new PdfViewerControl();

            _pdfViewer.Loaded += (sender, e) =>
            {
                try
                {
                    _pdfViewer.LoadPdf(_pdfPath);
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                context.Title = $"{Path.GetFileName(path)} (1 / {_pdfViewer.TotalPages})";
            };
            _pdfViewer.CurrentPageChanged += (sender, e) => context.Title =
                $"{Path.GetFileName(path)} ({_pdfViewer.CurrentPage + 1} / {_pdfViewer.TotalPages})";

            context.ViewerContent = _pdfViewer;

            context.IsBusy = false;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            // release the Pdf file first
            _pdfViewer?.Dispose();
            _pdfViewer = null;

            try
            {
                File.Delete(_pdfPath);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        ~PluginInterface()
        {
            Dispose();
        }
    }
}