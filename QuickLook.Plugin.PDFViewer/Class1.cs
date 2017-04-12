using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MoonPdfLib;

namespace QuickLook.Plugin.PDFViewer
{
    public class Class1:IViewer
    {
        public bool CanView(string path, byte[] sample)
        {
            if (String.IsNullOrEmpty(path))
                return false;
            
            if (Path.GetExtension(path).ToLower() == ".pdf")
                return true;

            if (Encoding.ASCII.GetString(sample.Take(4).ToArray()) == "%PDF")
                return true;

            return false;
        }

        public void View(string path, ViewContentContainer container)
        {
            MoonPdfPanel pdfPanel = new MoonPdfPanel
            {
                ViewType = ViewType.SinglePage,
                PageRowDisplay = PageRowDisplayType.ContinuousPageRows,
                PageMargin = new System.Windows.Thickness(0, 2, 4, 2),
                Background = new SolidColorBrush(Colors.LightGray)
            };

            container.SetContent(pdfPanel);

            Task.Delay(200).ContinueWith(t => container.Dispatcher.Invoke(() => pdfPanel.OpenFile(path)));

            Task.Delay(400).ContinueWith(t => container.Dispatcher.Invoke(() => pdfPanel.ZoomToWidth()));
        }

        public void Close()
        {
            return;
        }
    }
}
