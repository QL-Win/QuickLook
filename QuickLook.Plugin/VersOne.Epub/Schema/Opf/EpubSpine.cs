using System.Collections.Generic;

namespace VersOne.Epub.Schema
{
    public class EpubSpine : List<EpubSpineItemRef>
    {
        public string Toc { get; set; }
    }
}
