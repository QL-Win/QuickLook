using System;

namespace VersOne.Epub.Schema
{
    public class EpubManifestItem
    {
        public string Id { get; set; }
        public string Href { get; set; }
        public string MediaType { get; set; }
        public string RequiredNamespace { get; set; }
        public string RequiredModules { get; set; }
        public string Fallback { get; set; }
        public string FallbackStyle { get; set; }

        public override string ToString()
        {
            return String.Format("Id: {0}, Href = {1}, MediaType = {2}", Id, Href, MediaType);
        }
    }
}
