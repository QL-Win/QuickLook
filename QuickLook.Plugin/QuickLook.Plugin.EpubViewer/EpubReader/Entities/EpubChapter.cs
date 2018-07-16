using System;
using System.Collections.Generic;

namespace VersOne.Epub
{
    public class EpubChapter
    {
        public string Title { get; set; }
        public string ContentFileName { get; set; }
        public string Anchor { get; set; }
        public string HtmlContent { get; set; }
        public List<EpubChapter> SubChapters { get; set; }

        public override string ToString()
        {
            return String.Format("Title: {0}, Subchapter count: {1}", Title, SubChapters.Count);
        }
    }
}
