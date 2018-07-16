using System.Collections.Generic;
using VersOne.Epub.Schema;

namespace VersOne.Epub.Internal
{
    internal static class ContentReader
    {
        public static EpubContentRef ParseContentMap(EpubBookRef bookRef)
        {
            EpubContentRef result = new EpubContentRef
            {
                Html = new Dictionary<string, EpubTextContentFileRef>(),
                Css = new Dictionary<string, EpubTextContentFileRef>(),
                Images = new Dictionary<string, EpubByteContentFileRef>(),
                Fonts = new Dictionary<string, EpubByteContentFileRef>(),
                AllFiles = new Dictionary<string, EpubContentFileRef>()
            };
            foreach (EpubManifestItem manifestItem in bookRef.Schema.Package.Manifest)
            {
                string fileName = manifestItem.Href;
                string contentMimeType = manifestItem.MediaType;
                EpubContentType contentType = GetContentTypeByContentMimeType(contentMimeType);
                switch (contentType)
                {
                    case EpubContentType.XHTML_1_1:
                    case EpubContentType.CSS:
                    case EpubContentType.OEB1_DOCUMENT:
                    case EpubContentType.OEB1_CSS:
                    case EpubContentType.XML:
                    case EpubContentType.DTBOOK:
                    case EpubContentType.DTBOOK_NCX:
                        EpubTextContentFileRef epubTextContentFile = new EpubTextContentFileRef(bookRef)
                        {
                            FileName = fileName,
                            ContentMimeType = contentMimeType,
                            ContentType = contentType
                        };
                        switch (contentType)
                        {
                            case EpubContentType.XHTML_1_1:
                                result.Html[fileName] = epubTextContentFile;
                                break;
                            case EpubContentType.CSS:
                                result.Css[fileName] = epubTextContentFile;
                                break;
                        }
                        result.AllFiles[fileName] = epubTextContentFile;
                        break;
                    default:
                        EpubByteContentFileRef epubByteContentFile = new EpubByteContentFileRef(bookRef)
                        {
                            FileName = fileName,
                            ContentMimeType = contentMimeType,
                            ContentType = contentType
                        };
                        switch (contentType)
                        {
                            case EpubContentType.IMAGE_GIF:
                            case EpubContentType.IMAGE_JPEG:
                            case EpubContentType.IMAGE_PNG:
                            case EpubContentType.IMAGE_SVG:
                                result.Images[fileName] = epubByteContentFile;
                                break;
                            case EpubContentType.FONT_TRUETYPE:
                            case EpubContentType.FONT_OPENTYPE:
                                result.Fonts[fileName] = epubByteContentFile;
                                break;
                        }
                        result.AllFiles[fileName] = epubByteContentFile;
                        break;
                }
            }
            return result;
        }

        private static EpubContentType GetContentTypeByContentMimeType(string contentMimeType)
        {
            switch (contentMimeType.ToLowerInvariant())
            {
                case "application/xhtml+xml":
                    return EpubContentType.XHTML_1_1;
                case "application/x-dtbook+xml":
                    return EpubContentType.DTBOOK;
                case "application/x-dtbncx+xml":
                    return EpubContentType.DTBOOK_NCX;
                case "text/x-oeb1-document":
                    return EpubContentType.OEB1_DOCUMENT;
                case "application/xml":
                    return EpubContentType.XML;
                case "text/css":
                    return EpubContentType.CSS;
                case "text/x-oeb1-css":
                    return EpubContentType.OEB1_CSS;
                case "image/gif":
                    return EpubContentType.IMAGE_GIF;
                case "image/jpeg":
                    return EpubContentType.IMAGE_JPEG;
                case "image/png":
                    return EpubContentType.IMAGE_PNG;
                case "image/svg+xml":
                    return EpubContentType.IMAGE_SVG;
                case "font/truetype":
                    return EpubContentType.FONT_TRUETYPE;
                case "font/opentype":
                    return EpubContentType.FONT_OPENTYPE;
                case "application/vnd.ms-opentype":
                    return EpubContentType.FONT_OPENTYPE;
                default:
                    return EpubContentType.OTHER;
            }
        }
    }
}
