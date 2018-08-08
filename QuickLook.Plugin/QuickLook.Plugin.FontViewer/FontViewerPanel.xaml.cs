using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace QuickLook.Plugin.FontViewer
{
    public partial class FontViewerPanel : UserControl
    {
        public FontViewerPanel()
        {
            InitializeComponent();
            
            String TestString = "The quick brown fox jumped over the lazy brown dog";

            System.Windows.Thickness marginbottom = new System.Windows.Thickness(0, 0, 0, 5);

            var alpha = new Paragraph();
            alpha.Inlines.Add(new Run(string.Format("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ", Environment.TickCount)));
            alpha.Margin = marginbottom;
            RichTB.Document.Blocks.Add(alpha);

            var numeric = new Paragraph();
            numeric.Inlines.Add(new Run(string.Format("1234567890.:,;'\"(!?)+-*/=", Environment.TickCount)));
            numeric.Margin = marginbottom;
            RichTB.Document.Blocks.Add(numeric);


            var size12 = new Paragraph();
            size12.Inlines.Add(new Run(string.Format(TestString, Environment.TickCount)));
            size12.Margin = marginbottom;
            size12.FontSize = 12;
            RichTB.Document.Blocks.Add(size12);

            var size18 = new Paragraph();
            size18.Inlines.Add(new Run(string.Format(TestString, Environment.TickCount)));
            size18.FontSize = 18;
            size18.Margin = marginbottom;
            RichTB.Document.Blocks.Add(size18);

            var size24 = new Paragraph();
            size24.Inlines.Add(new Run(string.Format(TestString, Environment.TickCount)));
            size24.FontSize = 24;
            size24.Margin = marginbottom;
            RichTB.Document.Blocks.Add(size24);

            var size36 = new Paragraph();
            size36.Inlines.Add(new Run(string.Format(TestString, Environment.TickCount)));
            size36.FontSize = 36;
            size36.Margin = marginbottom;
            RichTB.Document.Blocks.Add(size36);
            
            var size48 = new Paragraph();
            size48.Inlines.Add(new Run(string.Format(TestString, Environment.TickCount)));
            size48.FontSize = 48;
            size48.Margin = marginbottom;
            RichTB.Document.Blocks.Add(size48);

            var size60 = new Paragraph();
            size60.Inlines.Add(new Run(string.Format(TestString, Environment.TickCount)));
            size60.FontSize = 60;
            size60.Margin = marginbottom;
            RichTB.Document.Blocks.Add(size60);
            
        }

        public void LoadFont(String path)
        {
            
            var a = Fonts.GetFontFamilies(path);
            
            GlyphTypeface gf = new GlyphTypeface(new Uri(path));

            String FontFamilyName = gf.FamilyNames.Values.FirstOrDefault();
            String FontFaceName = gf.FaceNames.Values.FirstOrDefault();
            String Manufacturer = gf.ManufacturerNames.Values.FirstOrDefault();
            String Copyright = gf.Copyrights.Values.FirstOrDefault();


           var FontName = new Paragraph();
            FontName.Inlines.Add(new Run(string.Format(FontFamilyName + " " + FontFaceName, Environment.TickCount)));
            FontName.FontSize = 45;
            FontName.FontFamily = a.ToArray()[0];
            FontName.Margin = new System.Windows.Thickness(0, 0, 0, 0);
            RichTBDetails.Document.Blocks.Add(FontName);

            if (Manufacturer != null)
            {
                var Foundry = new Paragraph();
                Foundry.Inlines.Add(new Run(string.Format("by " + Manufacturer, Environment.TickCount)));
                Foundry.FontSize = 14;
                Foundry.Margin = new System.Windows.Thickness(0, 0, 0, 0);
                RichTBDetails.Document.Blocks.Add(Foundry);
            }


            if (Copyright != null)
            {
                var CopyR = new Paragraph();
                CopyR.Inlines.Add(new Run(string.Format(Copyright, Environment.TickCount)));
                CopyR.FontSize = 14;
                CopyR.Margin = new System.Windows.Thickness(0, 0, 0, 0);
                RichTBDetails.Document.Blocks.Add(CopyR);
            }


            RichTB.FontFamily = a.ToArray()[0];

        }
    }
}
