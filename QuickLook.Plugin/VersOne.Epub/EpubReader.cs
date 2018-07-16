using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using VersOne.Epub.Internal;

namespace VersOne.Epub
{
    public static class EpubReader
    {
        /// <summary>
        /// Opens the book synchronously without reading its whole content. Holds the handle to the EPUB file.
        /// </summary>
        /// <param name="filePath">path to the EPUB file</param>
        /// <returns></returns>
        public static EpubBookRef OpenBook(string filePath)
        {
            return OpenBookAsync(filePath).Result;
        }

        /// <summary>
        /// Opens the book synchronously without reading its whole content.
        /// </summary>
        /// <param name="filePath">path to the EPUB file</param>
        /// <returns></returns>
        public static EpubBookRef OpenBook(Stream stream)
        {
            return OpenBookAsync(stream).Result;
        }

        /// <summary>
        /// Opens the book asynchronously without reading its whole content. Holds the handle to the EPUB file.
        /// </summary>
        /// <param name="filePath">path to the EPUB file</param>
        /// <returns></returns>
        public static Task<EpubBookRef> OpenBookAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Specified epub file not found.", filePath);
            }
            return OpenBookAsync(GetZipArchive(filePath));
        }

        /// <summary>
        /// Opens the book asynchronously without reading its whole content.
        /// </summary>
        /// <param name="filePath">path to the EPUB file</param>
        /// <returns></returns>
        public static Task<EpubBookRef> OpenBookAsync(Stream stream)
        {
            return OpenBookAsync(GetZipArchive(stream));
        }

        /// <summary>
        /// Opens the book synchronously and reads all of its content into the memory. Does not hold the handle to the EPUB file.
        /// </summary>
        /// <param name="filePath">path to the EPUB file</param>
        /// <returns></returns>
        public static EpubBook ReadBook(string filePath)
        {
            return ReadBookAsync(filePath).Result;
        }

        /// <summary>
        /// Opens the book synchronously and reads all of its content into the memory. Does not hold the handle to the EPUB file.
        /// </summary>
        /// <param name="filePath">path to the EPUB file</param>
        /// <returns></returns>
        public static EpubBook ReadBook(Stream stream)
        {
            return ReadBookAsync(stream).Result;
        }

        /// <summary>
        /// Opens the book asynchronously and reads all of its content into the memory. Does not hold the handle to the EPUB file.
        /// </summary>
        /// <param name="filePath">path to the EPUB file</param>
        /// <returns></returns>
        public static async Task<EpubBook> ReadBookAsync(string filePath)
        {
            EpubBookRef epubBookRef = await OpenBookAsync(filePath).ConfigureAwait(false);
            return await ReadBookAsync(epubBookRef).ConfigureAwait(false);
        }

        /// <summary>
        /// Opens the book asynchronously and reads all of its content into the memory.
        /// </summary>
        /// <param name="filePath">path to the EPUB file</param>
        /// <returns></returns>
        public static async Task<EpubBook> ReadBookAsync(Stream stream)
        {
            EpubBookRef epubBookRef = await OpenBookAsync(stream).ConfigureAwait(false);
            return await ReadBookAsync(epubBookRef).ConfigureAwait(false);
        }

        private static async Task<EpubBookRef> OpenBookAsync(ZipArchive zipArchive, string filePath = null)
        {
            EpubBookRef result = null;
            try
            {
                result = new EpubBookRef(zipArchive);
                result.FilePath = filePath;
                result.Schema = await SchemaReader.ReadSchemaAsync(zipArchive).ConfigureAwait(false);
                result.Title = result.Schema.Package.Metadata.Titles.FirstOrDefault() ?? String.Empty;
                result.AuthorList = result.Schema.Package.Metadata.Creators.Select(creator => creator.Creator).ToList();
                result.Author = String.Join(", ", result.AuthorList);
                result.Content = await Task.Run(() => ContentReader.ParseContentMap(result)).ConfigureAwait(false);
                return result;
            }
            catch
            {
                result?.Dispose();
                throw;
            }
        }

        private static async Task<EpubBook> ReadBookAsync(EpubBookRef epubBookRef)
        {
            EpubBook result = new EpubBook();
            using (epubBookRef)
            {
                result.FilePath = epubBookRef.FilePath;
                result.Schema = epubBookRef.Schema;
                result.Title = epubBookRef.Title;
                result.AuthorList = epubBookRef.AuthorList;
                result.Author = epubBookRef.Author;
                result.Content = await ReadContent(epubBookRef.Content).ConfigureAwait(false);
                result.CoverImage = await epubBookRef.ReadCoverAsync().ConfigureAwait(false);
                List<EpubChapterRef> chapterRefs = await epubBookRef.GetChaptersAsync().ConfigureAwait(false);
                result.Chapters = await ReadChapters(chapterRefs).ConfigureAwait(false);
            }
            return result;
        }

        private static ZipArchive GetZipArchive(string filePath)
        {
            return ZipFile.OpenRead(filePath);
        }

        private static ZipArchive GetZipArchive(Stream stream)
        {
            return new ZipArchive(stream, ZipArchiveMode.Read);
        }

        private static async Task<EpubContent> ReadContent(EpubContentRef contentRef)
        {
            EpubContent result = new EpubContent();
            result.Html = await ReadTextContentFiles(contentRef.Html).ConfigureAwait(false);
            result.Css = await ReadTextContentFiles(contentRef.Css).ConfigureAwait(false);
            result.Images = await ReadByteContentFiles(contentRef.Images).ConfigureAwait(false);
            result.Fonts = await ReadByteContentFiles(contentRef.Fonts).ConfigureAwait(false);
            result.AllFiles = new Dictionary<string, EpubContentFile>();
            foreach (KeyValuePair<string, EpubTextContentFile> textContentFile in result.Html.Concat(result.Css))
            {
                result.AllFiles.Add(textContentFile.Key, textContentFile.Value);
            }
            foreach (KeyValuePair<string, EpubByteContentFile> byteContentFile in result.Images.Concat(result.Fonts))
            {
                result.AllFiles.Add(byteContentFile.Key, byteContentFile.Value);
            }
            foreach (KeyValuePair<string, EpubContentFileRef> contentFileRef in contentRef.AllFiles)
            {
                if (!result.AllFiles.ContainsKey(contentFileRef.Key))
                {
                    result.AllFiles.Add(contentFileRef.Key, await ReadByteContentFile(contentFileRef.Value).ConfigureAwait(false));
                }
            }
            return result;
        }

        private static async Task<Dictionary<string, EpubTextContentFile>> ReadTextContentFiles(Dictionary<string, EpubTextContentFileRef> textContentFileRefs)
        {
            Dictionary<string, EpubTextContentFile> result = new Dictionary<string, EpubTextContentFile>();
            foreach (KeyValuePair<string, EpubTextContentFileRef> textContentFileRef in textContentFileRefs)
            {
                EpubTextContentFile textContentFile = new EpubTextContentFile
                {
                    FileName = textContentFileRef.Value.FileName,
                    ContentType = textContentFileRef.Value.ContentType,
                    ContentMimeType = textContentFileRef.Value.ContentMimeType
                };
                textContentFile.Content = await textContentFileRef.Value.ReadContentAsTextAsync().ConfigureAwait(false);
                result.Add(textContentFileRef.Key, textContentFile);
            }
            return result;
        }

        private static async Task<Dictionary<string, EpubByteContentFile>> ReadByteContentFiles(Dictionary<string, EpubByteContentFileRef> byteContentFileRefs)
        {
            Dictionary<string, EpubByteContentFile> result = new Dictionary<string, EpubByteContentFile>();
            foreach (KeyValuePair<string, EpubByteContentFileRef> byteContentFileRef in byteContentFileRefs)
            {
                result.Add(byteContentFileRef.Key, await ReadByteContentFile(byteContentFileRef.Value).ConfigureAwait(false));
            }
            return result;
        }

        private static async Task<EpubByteContentFile> ReadByteContentFile(EpubContentFileRef contentFileRef)
        {
            EpubByteContentFile result = new EpubByteContentFile
            {
                FileName = contentFileRef.FileName,
                ContentType = contentFileRef.ContentType,
                ContentMimeType = contentFileRef.ContentMimeType
            };
            result.Content = await contentFileRef.ReadContentAsBytesAsync().ConfigureAwait(false);
            return result;
        }

        private static async Task<List<EpubChapter>> ReadChapters(List<EpubChapterRef> chapterRefs)
        {
            List<EpubChapter> result = new List<EpubChapter>();
            foreach (EpubChapterRef chapterRef in chapterRefs)
            {
                EpubChapter chapter = new EpubChapter
                {
                    Title = chapterRef.Title,
                    ContentFileName = chapterRef.ContentFileName,
                    Anchor = chapterRef.Anchor
                };
                chapter.HtmlContent = await chapterRef.ReadHtmlContentAsync().ConfigureAwait(false);
                chapter.SubChapters = await ReadChapters(chapterRef.SubChapters).ConfigureAwait(false);
                result.Add(chapter);
            }
            return result;
        }
    }
}
