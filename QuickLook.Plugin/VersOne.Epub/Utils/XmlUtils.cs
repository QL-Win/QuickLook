using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace VersOne.Epub.Internal
{
    internal static class XmlUtils
    {
        public static async Task<XDocument> LoadDocumentAsync(Stream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                memoryStream.Position = 0;
                XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore,
                    Async = true
                };
                using (XmlReader xmlReader = XmlReader.Create(memoryStream, xmlReaderSettings))
                {
                    return await Task.Run(() => XDocument.Load(memoryStream)).ConfigureAwait(false);
                }
            }
        }
    }
}
