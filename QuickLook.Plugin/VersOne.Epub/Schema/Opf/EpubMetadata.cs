using System.Collections.Generic;

namespace VersOne.Epub.Schema
{
    public class EpubMetadata
    {
        public List<string> Titles { get; set; }
        public List<EpubMetadataCreator> Creators { get; set; }
        public List<string> Subjects { get; set; }
        public string Description { get; set; }
        public List<string> Publishers { get; set; }
        public List<EpubMetadataContributor> Contributors { get; set; }
        public List<EpubMetadataDate> Dates { get; set; }
        public List<string> Types { get; set; }
        public List<string> Formats { get; set; }
        public List<EpubMetadataIdentifier> Identifiers { get; set; }
        public List<string> Sources { get; set; }
        public List<string> Languages { get; set; }
        public List<string> Relations { get; set; }
        public List<string> Coverages { get; set; }
        public List<string> Rights { get; set; }
        public List<EpubMetadataMeta> MetaItems { get; set; }
    }
}
