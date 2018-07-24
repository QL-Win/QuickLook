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
using System.Linq;
using System.Threading.Tasks;

namespace VersOne.Epub.Internal
{
    internal static class BookCoverReader
    {
        public static async Task<byte[]> ReadBookCoverAsync(EpubBookRef bookRef)
        {
            var metaItems = bookRef.Schema.Package.Metadata.MetaItems;
            if (metaItems == null || !metaItems.Any()) return null;
            var coverMetaItem = metaItems.FirstOrDefault(metaItem =>
                string.Compare(metaItem.Name, "cover", StringComparison.OrdinalIgnoreCase) == 0);
            if (coverMetaItem == null) return null;
            if (string.IsNullOrEmpty(coverMetaItem.Content))
                throw new Exception("Incorrect EPUB metadata: cover item content is missing.");
            var coverManifestItem = bookRef.Schema.Package.Manifest.FirstOrDefault(manifestItem =>
                string.Compare(manifestItem.Id, coverMetaItem.Content, StringComparison.OrdinalIgnoreCase) == 0);
            if (coverManifestItem == null)
                throw new Exception(string.Format("Incorrect EPUB manifest: item with ID = \"{0}\" is missing.",
                    coverMetaItem.Content));
            if (!bookRef.Content.Images.TryGetValue(coverManifestItem.Href, out var coverImageContentFileRef))
                throw new Exception(string.Format("Incorrect EPUB manifest: item with href = \"{0}\" is missing.",
                    coverManifestItem.Href));
            var coverImageContent = await coverImageContentFileRef.ReadContentAsBytesAsync().ConfigureAwait(false);
            return coverImageContent;
        }
    }
}