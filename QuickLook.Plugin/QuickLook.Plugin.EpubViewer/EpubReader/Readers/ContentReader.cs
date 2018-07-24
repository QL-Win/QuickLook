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

using System.Collections.Generic;

namespace VersOne.Epub.Internal
{
    internal static class ContentReader
    {
        public static EpubContentRef ParseContentMap(EpubBookRef bookRef)
        {
            var result = new EpubContentRef
            {
                Html = new Dictionary<string, EpubTextContentFileRef>(),
                Css = new Dictionary<string, EpubTextContentFileRef>(),
                Images = new Dictionary<string, EpubByteContentFileRef>(),
                Fonts = new Dictionary<string, EpubByteContentFileRef>(),
                AllFiles = new Dictionary<string, EpubContentFileRef>()
            };
            foreach (var manifestItem in bookRef.Schema.Package.Manifest)
            {
                var fileName = manifestItem.Href;
                var contentMimeType = manifestItem.MediaType;
                var contentType = GetContentTypeByContentMimeType(contentMimeType);
                switch (contentType)
                {
                    case EpubContentType.XHTML_1_1:
                    case EpubContentType.CSS:
                    case EpubContentType.OEB1_DOCUMENT:
                    case EpubContentType.OEB1_CSS:
                    case EpubContentType.XML:
                    case EpubContentType.DTBOOK:
                    case EpubContentType.DTBOOK_NCX:
                        var epubTextContentFile = new EpubTextContentFileRef(bookRef)
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
                        var epubByteContentFile = new EpubByteContentFileRef(bookRef)
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