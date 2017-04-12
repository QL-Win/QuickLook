using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using MoonPdfLib;
using QuickLook.ExtensionMethods;

namespace QuickLook.Plugin.PDFViewer
{
    public class Class1 : IViewer
    {
        public PluginType Type => PluginType.ByExtension | PluginType.ByContent;

        public string[] SupportExtensions => new[] {".pdf"};

        public bool CheckSupportByContent(byte[] sample)
        {
            return Encoding.ASCII.GetString(sample.Take(4).ToArray()) == "%PDF";
        }

        public void View(string path, ViewContentContainer container)
        {
            var pdfPanel = new MoonPdfPanel
            {
                ViewType = ViewType.SinglePage,
                PageRowDisplay = PageRowDisplayType.ContinuousPageRows,
                PageMargin = new Thickness(0, 2, 4, 2),
                Background = new SolidColorBrush(Colors.LightGray)
            };
            container.SetContent(pdfPanel);

            container.Dispatcher.Delay(100, o => pdfPanel.OpenFile(path));
            //container.Dispatcher.Delay(200, o => pdfPanel.ZoomToWidth());
        }

        public void Close()
        {
        }
    }
}