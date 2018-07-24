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
using System.IO.Compression;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace VersOne.Epub.Internal
{
    internal static class RootFilePathReader
    {
        public static async Task<string> GetRootFilePathAsync(ZipArchive epubArchive)
        {
            const string EPUB_CONTAINER_FILE_PATH = "META-INF/container.xml";
            var containerFileEntry = epubArchive.GetEntry(EPUB_CONTAINER_FILE_PATH);
            if (containerFileEntry == null)
                throw new Exception(string.Format("EPUB parsing error: {0} file not found in archive.",
                    EPUB_CONTAINER_FILE_PATH));
            XDocument containerDocument;
            using (var containerStream = containerFileEntry.Open())
            {
                containerDocument = await XmlUtils.LoadDocumentAsync(containerStream).ConfigureAwait(false);
            }

            XNamespace cnsNamespace = "urn:oasis:names:tc:opendocument:xmlns:container";
            var fullPathAttribute = containerDocument.Element(cnsNamespace + "container")
                ?.Element(cnsNamespace + "rootfiles")?.Element(cnsNamespace + "rootfile")?.Attribute("full-path");
            if (fullPathAttribute == null)
                throw new Exception("EPUB parsing error: root file path not found in the EPUB container.");
            return fullPathAttribute.Value;
        }
    }
}