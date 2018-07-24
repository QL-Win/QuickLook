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
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using VersOne.Epub.Internal;

namespace VersOne.Epub
{
    public abstract class EpubContentFileRef
    {
        private readonly EpubBookRef epubBookRef;

        public EpubContentFileRef(EpubBookRef epubBookRef)
        {
            this.epubBookRef = epubBookRef;
        }

        public string FileName { get; set; }
        public EpubContentType ContentType { get; set; }
        public string ContentMimeType { get; set; }

        public byte[] ReadContentAsBytes()
        {
            return ReadContentAsBytesAsync().Result;
        }

        public async Task<byte[]> ReadContentAsBytesAsync()
        {
            var contentFileEntry = GetContentFileEntry();
            var content = new byte[(int) contentFileEntry.Length];
            using (var contentStream = OpenContentStream(contentFileEntry))
            using (var memoryStream = new MemoryStream(content))
            {
                await contentStream.CopyToAsync(memoryStream).ConfigureAwait(false);
            }

            return content;
        }

        public string ReadContentAsText()
        {
            return ReadContentAsTextAsync().Result;
        }

        public async Task<string> ReadContentAsTextAsync()
        {
            using (var contentStream = GetContentStream())
            using (var streamReader = new StreamReader(contentStream))
            {
                return await streamReader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        public Stream GetContentStream()
        {
            return OpenContentStream(GetContentFileEntry());
        }

        private ZipArchiveEntry GetContentFileEntry()
        {
            var contentFilePath = ZipPathUtils.Combine(epubBookRef.Schema.ContentDirectoryPath, FileName);
            var contentFileEntry = epubBookRef.EpubArchive.GetEntry(contentFilePath);
            if (contentFileEntry == null)
                throw new Exception(
                    string.Format("EPUB parsing error: file {0} not found in archive.", contentFilePath));
            if (contentFileEntry.Length > int.MaxValue)
                throw new Exception(string.Format("EPUB parsing error: file {0} is bigger than 2 Gb.",
                    contentFilePath));
            return contentFileEntry;
        }

        private Stream OpenContentStream(ZipArchiveEntry contentFileEntry)
        {
            var contentStream = contentFileEntry.Open();
            if (contentStream == null)
                throw new Exception(string.Format(
                    "Incorrect EPUB file: content file \"{0}\" specified in manifest is not found.", FileName));
            return contentStream;
        }
    }
}