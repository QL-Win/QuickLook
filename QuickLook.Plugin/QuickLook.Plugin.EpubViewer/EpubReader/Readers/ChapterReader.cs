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
using System.Linq;
using System.Net;
using VersOne.Epub.Schema;

namespace VersOne.Epub.Internal
{
    internal static class ChapterReader
    {
        public static List<EpubChapterRef> GetChapters(EpubBookRef bookRef)
        {
            return GetChapters(bookRef, bookRef.Schema.Package.Spine, bookRef.Schema.Navigation.NavMap);
        }

        public static List<EpubChapterRef> GetChapters(EpubBookRef bookRef, EpubSpine spine,
            List<EpubNavigationPoint> navigationPoints)
        {
            var result = new List<EpubChapterRef>();
            for (var s = 0; s < spine.Count; s++)
            {
                var itemRef = spine[s];
                string contentFileName;
                string anchor;
                contentFileName = WebUtility.UrlDecode(bookRef.Schema.Package.Manifest
                    .FirstOrDefault(e => e.Id == itemRef.IdRef)?.Href);
                anchor = null;
                if (!bookRef.Content.Html.TryGetValue(contentFileName, out var htmlContentFileRef))
                    throw new Exception(string.Format("Incorrect EPUB manifest: item with href = \"{0}\" is missing.",
                        contentFileName));
                var chapterRef = new EpubChapterRef(htmlContentFileRef);
                chapterRef.ContentFileName = contentFileName;
                chapterRef.Anchor = anchor;
                chapterRef.Parent = null;
                var navPoint = navigationPoints.LastOrDefault(nav =>
                    spine.Take(s + 1)
                        .Select(sp => bookRef.Schema.Package.Manifest.FirstOrDefault(e => e.Id == sp.IdRef)?.Href)
                        .Contains(nav.Content.Source.Split('#')[0]));
                if (navPoint != null)
                    chapterRef.Title = navPoint.NavigationLabels.First().Text;
                else
                    chapterRef.Title = $"Chapter {s + 1}";
                chapterRef.SubChapters = new List<EpubChapterRef>();
                result.Add(chapterRef);
            }

            return result;
        }
    }
}