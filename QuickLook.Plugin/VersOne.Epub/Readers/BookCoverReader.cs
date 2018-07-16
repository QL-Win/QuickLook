using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VersOne.Epub.Schema;

namespace VersOne.Epub.Internal
{
    internal static class BookCoverReader
    {
        public static async Task<byte[]> ReadBookCoverAsync(EpubBookRef bookRef)
        {
            List<EpubMetadataMeta> metaItems = bookRef.Schema.Package.Metadata.MetaItems;
            if (metaItems == null || !metaItems.Any())
            {
                return null;
            }
            EpubMetadataMeta coverMetaItem = metaItems.FirstOrDefault(metaItem => String.Compare(metaItem.Name, "cover", StringComparison.OrdinalIgnoreCase) == 0);
            if (coverMetaItem == null)
            {
                return null;
            }
            if (String.IsNullOrEmpty(coverMetaItem.Content))
            {
                throw new Exception("Incorrect EPUB metadata: cover item content is missing.");
            }
            EpubManifestItem coverManifestItem = bookRef.Schema.Package.Manifest.FirstOrDefault(manifestItem => String.Compare(manifestItem.Id, coverMetaItem.Content, StringComparison.OrdinalIgnoreCase) == 0);
            if (coverManifestItem == null)
            {
                throw new Exception(String.Format("Incorrect EPUB manifest: item with ID = \"{0}\" is missing.", coverMetaItem.Content));
            }
            if (!bookRef.Content.Images.TryGetValue(coverManifestItem.Href, out EpubByteContentFileRef coverImageContentFileRef))
            {
                throw new Exception(String.Format("Incorrect EPUB manifest: item with href = \"{0}\" is missing.", coverManifestItem.Href));
            }
            byte[] coverImageContent = await coverImageContentFileRef.ReadContentAsBytesAsync().ConfigureAwait(false);
            return coverImageContent;
        }
    }
}
