using System.Threading.Tasks;

namespace VersOne.Epub
{
    public class EpubTextContentFileRef : EpubContentFileRef
    {
        public EpubTextContentFileRef(EpubBookRef epubBookRef)
            : base(epubBookRef)
        {
        }

        public string ReadContent()
        {
            return ReadContentAsText();
        }

        public Task<string> ReadContentAsync()
        {
            return ReadContentAsTextAsync();
        }
    }
}
