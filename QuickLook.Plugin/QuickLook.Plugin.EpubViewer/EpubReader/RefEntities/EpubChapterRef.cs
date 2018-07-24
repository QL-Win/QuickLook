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
using System.Threading.Tasks;

namespace VersOne.Epub
{
    public class EpubChapterRef
    {
        private readonly EpubTextContentFileRef epubTextContentFileRef;

        public EpubChapterRef(EpubTextContentFileRef epubTextContentFileRef)
        {
            this.epubTextContentFileRef = epubTextContentFileRef;
        }

        public string Title { get; set; }
        public string ContentFileName { get; set; }
        public string Anchor { get; set; }
        public List<EpubChapterRef> SubChapters { get; set; }
        public EpubChapterRef Parent { get; set; }

        public string ReadHtmlContent()
        {
            return ReadHtmlContentAsync().Result;
        }

        public Task<string> ReadHtmlContentAsync()
        {
            return epubTextContentFileRef.ReadContentAsTextAsync();
        }

        public override string ToString()
        {
            return string.Format("Title: {0}, Subchapter count: {1}", Title, SubChapters.Count);
        }
    }
}