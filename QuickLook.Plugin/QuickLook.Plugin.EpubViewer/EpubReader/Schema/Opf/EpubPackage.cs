namespace VersOne.Epub.Schema
{
    public class EpubPackage
    {
        public EpubVersion EpubVersion { get; set; }
        public EpubMetadata Metadata { get; set; }
        public EpubManifest Manifest { get; set; }
        public EpubSpine Spine { get; set; }
        public EpubGuide Guide { get; set; }
    }
}
