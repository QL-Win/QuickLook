// Copyright © 2018 Marco Gavelli and Paddy Xu
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
using System.Collections.Generic;
using System.IO.Compression;
using System.Threading.Tasks;
using VersOne.Epub.Internal;

namespace VersOne.Epub
{
    public class EpubBookRef : IDisposable
    {
        private bool isDisposed;

        public EpubBookRef(ZipArchive epubArchive)
        {
            EpubArchive = epubArchive;
            isDisposed = false;
        }

        public string FilePath { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public List<string> AuthorList { get; set; }
        public EpubSchema Schema { get; set; }
        public EpubContentRef Content { get; set; }

        internal ZipArchive EpubArchive { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EpubBookRef()
        {
            Dispose(false);
        }

        public byte[] ReadCover()
        {
            return ReadCoverAsync().Result;
        }

        public async Task<byte[]> ReadCoverAsync()
        {
            return await BookCoverReader.ReadBookCoverAsync(this).ConfigureAwait(false);
        }

        public List<EpubChapterRef> GetChapters()
        {
            return GetChaptersAsync().Result;
        }

        public async Task<List<EpubChapterRef>> GetChaptersAsync()
        {
            return await Task.Run(() => ChapterReader.GetChapters(this)).ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing) EpubArchive?.Dispose();
                isDisposed = true;
            }
        }
    }
}