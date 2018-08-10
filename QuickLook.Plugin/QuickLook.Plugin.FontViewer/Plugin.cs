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
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Windows;
using QuickLook.Common.Plugin;
using System.Windows.Media;
using System.Linq;
using System.Globalization;

namespace QuickLook.Plugin.FontViewer
{
    public class Plugin : IViewer
    {

        private FontViewerPanel _fontPanel;

        public int Priority => int.MaxValue;

        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            string[] Extensions = { ".ttf", ".otf" };

            return !Directory.Exists(path) && Extensions.Any(path.ToLower().EndsWith);
        }

        public void Prepare(string path, ContextObject context)
        {
            context.PreferredSize = new Size { Width = 1300, Height = 500 };
        }

        public void View(string path, ContextObject context)
        {
          
            GlyphTypeface gf = new GlyphTypeface(new Uri(path));

            String FontFamilyName = gf.FamilyNames[CultureInfo.CurrentCulture] != null ? gf.FamilyNames[CultureInfo.CurrentCulture] : gf.FamilyNames.Values.FirstOrDefault();
            String FontFaceName = gf.FaceNames[CultureInfo.CurrentCulture] != null ? gf.FaceNames[CultureInfo.CurrentCulture] : gf.FaceNames.Values.FirstOrDefault();

            _fontPanel = new FontViewerPanel();
            context.ViewerContent = _fontPanel;
            context.Title = FontFamilyName + " " + FontFaceName;

            _fontPanel.LoadFont(path);

            context.IsBusy = false;


        }

        public void Cleanup()
        {
            GC.SuppressFinalize(this);

           
        }

        
    }
}