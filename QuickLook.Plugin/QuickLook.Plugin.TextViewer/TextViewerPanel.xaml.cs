// Copyright © 2017 Paddy Xu
// 
// This file is part of QuickLook program.
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

using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using QuickLook.Plugin.TextViewer.SimpleHelpers;

namespace QuickLook.Plugin.TextViewer
{
    /// <summary>
    ///     Interaction logic for TextViewerPanel.xaml
    /// </summary>
    public partial class TextViewerPanel : UserControl
    {
        public TextViewerPanel(string path, ContextObject context)
        {
            InitializeComponent();

            viewer.FontFamily =
                new FontFamily(context.GetString(
                    Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Translations.lang"),
                    "Editor_FontFamily", failsafe: "Consolas"));

            LoadFile(path);
        }

        private void LoadFile(string path)
        {
            using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                viewer.Encoding = DetectEncoding(s);
                viewer.Load(path);
            }

            viewer.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(path));
        }

        private static Encoding DetectEncoding(Stream s)
        {
            var det = new FileEncoding();
            det.Detect(s, 1 * 1024 * 1024);
            return det.Complete() ?? Encoding.Default;
        }
    }
}