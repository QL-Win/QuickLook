using System.Collections.Generic;

namespace VersOne.Epub.Schema
{
    public class EpubNavigationPageTarget
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public EpubNavigationPageTargetType Type { get; set; }
        public string Class { get; set; }
        public string PlayOrder { get; set; }
        public List<EpubNavigationLabel> NavigationLabels { get; set; }
        public EpubNavigationContent Content { get; set; }
    }
}
