using System;
using System.Drawing;
using Size = System.Windows.Size;

namespace QuickLook.Plugin.PDFViewer
{
    public class PdfFile : IDisposable
    {
        private readonly IntPtr _ctx;
        private readonly IntPtr _doc;
        private readonly IntPtr _stm;

        public PdfFile(string path)
        {
            _ctx = LibMuPdf.NativeMethods.NewContext();
            _stm = LibMuPdf.NativeMethods.OpenFile(_ctx, path);
            _doc = LibMuPdf.NativeMethods.OpenDocumentStream(_ctx, _stm);

            TotalPages = LibMuPdf.NativeMethods.CountPages(_doc);
        }

        public int TotalPages { get; }

        public void Dispose()
        {
            LibMuPdf.NativeMethods.CloseDocument(_doc);
            LibMuPdf.NativeMethods.CloseStream(_stm);
            LibMuPdf.NativeMethods.FreeContext(_ctx);
        }

        public bool IsLastPage(int pageId)
        {
            return pageId >= TotalPages;
        }

        public Size GetPageSize(int pageId, double zoomFactor)
        {
            if (pageId < 0 || pageId >= TotalPages)
                throw new OverflowException(
                    $"Page id {pageId} should greater or equal than 0 and less than total page count {TotalPages}.");

            var p = LibMuPdf.NativeMethods.LoadPage(_doc, pageId);

            var realSize = new LibMuPdf.Rectangle();
            LibMuPdf.NativeMethods.BoundPage(_doc, p, ref realSize);

            var size = new Size
            {
                Width = realSize.Right * zoomFactor,
                Height = realSize.Bottom * zoomFactor
            };

            LibMuPdf.NativeMethods.FreePage(_doc, p);

            return size;
        }

        public Bitmap GetPage(int pageId, double zoomFactor)
        {
            if (pageId < 0 || pageId >= TotalPages)
                throw new OverflowException(
                    $"Page id {pageId} should greater or equal than 0 and less than total page count {TotalPages}.");

            var p = LibMuPdf.NativeMethods.LoadPage(_doc, pageId);

            var bmp = LibMuPdf.RenderPage(_ctx, _doc, p, zoomFactor);

            LibMuPdf.NativeMethods.FreePage(_doc, p);

            return bmp;
        }
    }
}