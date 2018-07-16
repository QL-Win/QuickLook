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
            // return GetChapters(bookRef, bookRef.Schema.Navigation.NavMap);
            return GetChapters(bookRef, bookRef.Schema.Package.Spine, bookRef.Schema.Navigation.NavMap);
        }

        public static List<EpubChapterRef> GetChapters(EpubBookRef bookRef, EpubSpine spine, List<EpubNavigationPoint> navigationPoints)
        {
            List<EpubChapterRef> result = new List<EpubChapterRef>();
            for (int s = 0; s < spine.Count; s++)
            {
                EpubSpineItemRef itemRef = spine[s];
                string contentFileName;
                string anchor;
                contentFileName = WebUtility.UrlDecode(bookRef.Schema.Package.Manifest.FirstOrDefault(e => e.Id == itemRef.IdRef)?.Href);
                anchor = null;
                if (!bookRef.Content.Html.TryGetValue(contentFileName, out EpubTextContentFileRef htmlContentFileRef))
                {
                    throw new Exception(String.Format("Incorrect EPUB manifest: item with href = \"{0}\" is missing.", contentFileName));
                }
                EpubChapterRef chapterRef = new EpubChapterRef(htmlContentFileRef);
                chapterRef.ContentFileName = contentFileName;
                chapterRef.Anchor = anchor;
                chapterRef.Parent = null;
                var navPoint = navigationPoints.LastOrDefault(nav => spine.Take(s + 1).Select(sp => bookRef.Schema.Package.Manifest.FirstOrDefault(e => e.Id == sp.IdRef)?.Href).Contains(nav.Content.Source.Split('#')[0]));
                if (navPoint != null)
                {
                    chapterRef.Title = navPoint.NavigationLabels.First().Text;
                }
                else
                {
                    chapterRef.Title = $"Chapter {s + 1}";
                }                
                chapterRef.SubChapters = new List<EpubChapterRef>();
                result.Add(chapterRef);
            }
            return result;
        }

        public static List<EpubChapterRef> GetChapters(EpubBookRef bookRef, List<EpubNavigationPoint> navigationPoints, EpubChapterRef parentChapter = null)
        {
            List<EpubChapterRef> result = new List<EpubChapterRef>();
            foreach (EpubNavigationPoint navigationPoint in navigationPoints)
            {
                string contentFileName;
                string anchor;
                int contentSourceAnchorCharIndex = navigationPoint.Content.Source.IndexOf('#');
                if (contentSourceAnchorCharIndex == -1)
                {
                    contentFileName = WebUtility.UrlDecode(navigationPoint.Content.Source);
                    anchor = null;
                }
                else
                {
                    contentFileName = WebUtility.UrlDecode(navigationPoint.Content.Source.Substring(0, contentSourceAnchorCharIndex));
                    anchor = navigationPoint.Content.Source.Substring(contentSourceAnchorCharIndex + 1);
                }
                if (!bookRef.Content.Html.TryGetValue(contentFileName, out EpubTextContentFileRef htmlContentFileRef))
                {
                    throw new Exception(String.Format("Incorrect EPUB manifest: item with href = \"{0}\" is missing.", contentFileName));
                }
                EpubChapterRef chapterRef = new EpubChapterRef(htmlContentFileRef);
                chapterRef.ContentFileName = contentFileName;
                chapterRef.Anchor = anchor;
                chapterRef.Parent = parentChapter;
                chapterRef.Title = navigationPoint.NavigationLabels.First().Text;
                chapterRef.SubChapters = GetChapters(bookRef, navigationPoint.ChildNavigationPoints, chapterRef);
                result.Add(chapterRef);
            }
            return result;
        }
    }
}
