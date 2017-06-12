// Copyright © 2017 Paddy Xu
// 
// This file is part of QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
            if (App.Is64Bit)
            {
                _ctx = LibMuPdf.NativeMethods.NewContext();
                _stm = LibMuPdf.NativeMethods.OpenFile_64(_ctx, path);
                _doc = LibMuPdf.NativeMethods.OpenDocumentStream_64(_ctx, _stm);
                TotalPages = LibMuPdf.NativeMethods.CountPages_64(_doc);
            }
            else
            {
                _ctx = LibMuPdf.NativeMethods.NewContext();
                _stm = LibMuPdf.NativeMethods.OpenFile_32(_ctx, path);
                _doc = LibMuPdf.NativeMethods.OpenDocumentStream_32(_ctx, _stm);
                TotalPages = LibMuPdf.NativeMethods.CountPages_32(_doc);
            }
        }

        public int TotalPages { get; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (App.Is64Bit)
            {
                LibMuPdf.NativeMethods.CloseDocument_64(_doc);
                LibMuPdf.NativeMethods.CloseStream_64(_stm);
                LibMuPdf.NativeMethods.FreeContext_64(_ctx);
            }
            else
            {
                LibMuPdf.NativeMethods.CloseDocument_32(_doc);
                LibMuPdf.NativeMethods.CloseStream_32(_stm);
                LibMuPdf.NativeMethods.FreeContext_32(_ctx);
            }
        }

        ~PdfFile()
        {
            Dispose();
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

            var p = App.Is64Bit
                ? LibMuPdf.NativeMethods.LoadPage_64(_doc, pageId)
                : LibMuPdf.NativeMethods.LoadPage_32(_doc, pageId);

            var realSize = new LibMuPdf.Rectangle();
            if (App.Is64Bit)
                LibMuPdf.NativeMethods.BoundPage_64(_doc, p, ref realSize);
            else
                LibMuPdf.NativeMethods.BoundPage_32(_doc, p, ref realSize);

            var size = new Size
            {
                Width = realSize.Right * zoomFactor,
                Height = realSize.Bottom * zoomFactor
            };

            if (App.Is64Bit)
                LibMuPdf.NativeMethods.FreePage_64(_doc, p);
            else
                LibMuPdf.NativeMethods.FreePage_32(_doc, p);

            return size;
        }

        public Bitmap GetPage(int pageId, double zoomFactor)
        {
            if (pageId < 0 || pageId >= TotalPages)
                throw new OverflowException(
                    $"Page id {pageId} should greater or equal than 0 and less than total page count {TotalPages}.");

            var p = App.Is64Bit
                ? LibMuPdf.NativeMethods.LoadPage_64(_doc, pageId)
                : LibMuPdf.NativeMethods.LoadPage_32(_doc, pageId);

            var bmp = LibMuPdf.RenderPage(_ctx, _doc, p, zoomFactor);

            if (App.Is64Bit)
                LibMuPdf.NativeMethods.FreePage_64(_doc, p);
            else
                LibMuPdf.NativeMethods.FreePage_32(_doc, p);

            return bmp;
        }
    }
}