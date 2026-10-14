// Copyright © 2017-2026 QL-Win Contributors
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
using System.IO;
using System.Linq;
using PdfiumViewer;

namespace QuickLook.Plugin.PDFViewer;

public class PdfDocumentWrapper : IDisposable
{
    private readonly string _path;
    private readonly string _password;
    private SizeF[] _pageSizes;
    private bool _disposed;

    public PdfDocumentWrapper(string path, string password = null)
    {
        _path = path;
        _password = password;
        PdfDocument = PdfDocument.Load(path, password);
        _pageSizes = PdfDocument.PageSizes.ToArray();
        PageCount = PdfDocument.PageCount;
    }

    public PdfDocumentWrapper(Stream stream, string password = null)
    {
        _password = password;
        PdfStream = new MemoryStream((int)stream.Length);
        stream.CopyTo(PdfStream);
        PdfStream.Position = 0;

        PdfDocument = PdfDocument.Load(PdfStream, password);
        _pageSizes = PdfDocument.PageSizes.ToArray();
        PageCount = PdfDocument.PageCount;
    }

    public PdfDocument PdfDocument { get; private set; }

    public MemoryStream PdfStream { get; private set; }

    public int PageCount { get; private set; }

    public SizeF GetPageSize(int page) => _pageSizes[page];

    public void Dispose()
    {
        lock (PdfPageExtension.RenderLock)
        {
            if (_disposed)
                return;

            _disposed = true;
            PdfDocument?.Dispose();
            PdfDocument = null;
            PdfStream?.Dispose();
            PdfStream = null;
        }
    }

    public void Refresh()
    {
        lock (PdfPageExtension.RenderLock)
        {
            if (_disposed)
                return;

            if (PdfStream != null)
                PdfStream.Position = 0;

            var newDocument = _path != null
                ? PdfDocument.Load(_path, _password)
                : PdfDocument.Load(PdfStream, _password);
            var newPageSizes = newDocument.PageSizes.ToArray();
            var oldDocument = PdfDocument;

            PdfDocument = newDocument;
            PageCount = newDocument.PageCount;
            _pageSizes = newPageSizes;

            oldDocument.Dispose();
        }
    }
}
