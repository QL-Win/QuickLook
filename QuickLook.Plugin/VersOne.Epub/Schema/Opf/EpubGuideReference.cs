using System;

namespace VersOne.Epub.Schema
{
    public class EpubGuideReference
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public string Href { get; set; }

        public override string ToString()
        {
            return String.Format("Type: {0}, Href: {1}", Type, Href);
        }
    }
}
