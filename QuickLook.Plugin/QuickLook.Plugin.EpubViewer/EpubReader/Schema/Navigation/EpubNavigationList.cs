using System.Collections.Generic;

namespace VersOne.Epub.Schema
{
    public class EpubNavigationList
    {
        public string Id { get; set; }
        public string Class { get; set; }
        public List<EpubNavigationLabel> NavigationLabels { get; set; }
        public List<EpubNavigationTarget> NavigationTargets { get; set; }
    }
}
