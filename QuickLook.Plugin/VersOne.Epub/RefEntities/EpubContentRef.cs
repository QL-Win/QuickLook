using System.Collections.Generic;

namespace VersOne.Epub
{
    public class EpubContentRef
    {
        public Dictionary<string, EpubTextContentFileRef> Html { get; set; }
        public Dictionary<string, EpubTextContentFileRef> Css { get; set; }
        public Dictionary<string, EpubByteContentFileRef> Images { get; set; }
        public Dictionary<string, EpubByteContentFileRef> Fonts { get; set; }
        public Dictionary<string, EpubContentFileRef> AllFiles { get; set; }
    }
}
