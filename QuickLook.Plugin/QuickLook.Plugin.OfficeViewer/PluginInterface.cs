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

        public void Prepare(string path, ViewContentContainer container)
        {
            container.SetPreferedSizeFit(new Size {Width = 800, Height = 600}, 0.8);
        }

        public void View(string path, ViewContentContainer container)
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

                container.Title = $"{Path.GetFileName(path)} (1 / {_pdfViewer.TotalPages})";
            };
            _pdfViewer.CurrentPageChanged += (sender, e) => container.Title =
                $"{Path.GetFileName(path)} ({_pdfViewer.CurrectPage + 1} / {_pdfViewer.TotalPages})";

            container.SetContent(_pdfViewer);
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