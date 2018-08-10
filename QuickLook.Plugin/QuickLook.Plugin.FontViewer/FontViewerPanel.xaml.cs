// Copyright © 2018 Jeremy Hart
// 
// This file a plugin for the QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>

using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Globalization;
using QuickLook.Common.Helpers;

namespace QuickLook.Plugin.FontViewer
{
    public partial class FontViewerPanel : UserControl
    {
        public FontViewerPanel()
        {
            InitializeComponent();

         
            var TestString = TranslationHelper.Get("SAMPLE_TEXT");

            System.Windows.Thickness marginbottom = new System.Windows.Thickness(0, 0, 0, 5);

            var alpha = new Paragraph();
            alpha.Inlines.Add(new Run(TranslationHelper.Get("ALPHA")));
            alpha.Margin = marginbottom;
            RichTB.Document.Blocks.Add(alpha);

            var numeric = new Paragraph();
            numeric.Inlines.Add(new Run(TranslationHelper.Get("DIGITS") + TranslationHelper.Get("PUNCTUATION") + TranslationHelper.Get("SYMBOLS")));
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

          
            String FontFamilyName = gf.FamilyNames[CultureInfo.CurrentCulture] != null ? gf.FamilyNames[CultureInfo.CurrentCulture] : gf.FamilyNames.Values.FirstOrDefault();
            String FontFaceName = gf.FaceNames[CultureInfo.CurrentCulture] !=null? gf.FaceNames[CultureInfo.CurrentCulture] : gf.FaceNames.Values.FirstOrDefault();
            String Manufacturer = gf.ManufacturerNames[CultureInfo.CurrentCulture] != null ? gf.ManufacturerNames[CultureInfo.CurrentCulture] : gf.ManufacturerNames.Values.FirstOrDefault();
            String Copyright = gf.Copyrights[CultureInfo.CurrentCulture] != null ? gf.Copyrights[CultureInfo.CurrentCulture] : gf.Copyrights.Values.FirstOrDefault();


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
