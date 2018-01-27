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
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using QuickLook.Common;
using UtfUnknown;

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

            viewer.ManipulationInertiaStarting += Viewer_ManipulationInertiaStarting;
            viewer.ManipulationStarting += Viewer_ManipulationStarting;
            viewer.ManipulationDelta += Viewer_ManipulationDelta;

            viewer.PreviewMouseWheel += Viewer_MouseWheel;

            viewer.FontFamily =
                new FontFamily(context.GetString("Editor_FontFamily"));

            LoadFile(path);
        }

        private void Viewer_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            e.TranslationBehavior = new InertiaTranslationBehavior
            {
                InitialVelocity = e.InitialVelocities.LinearVelocity,
                DesiredDeceleration = 10.0 * 96.0 / (1000.0 * 1000.0)
            };
        }

        private void Viewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            viewer.ScrollToVerticalOffset(viewer.VerticalOffset - e.Delta);
        }

        private void Viewer_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            e.Handled = true;

            var delta = e.DeltaManipulation;
            viewer.ScrollToVerticalOffset(viewer.VerticalOffset - delta.Translation.Y);
        }

        private void Viewer_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = this;
            e.Mode = ManipulationModes.Translate;
        }

        private void LoadFile(string path)
        {
            using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                const int bufferLength = 1 * 1024 * 1024;
                var buffer = new byte[bufferLength];
                s.Read(buffer, 0, bufferLength);

                viewer.Encoding = CharsetDetector.DetectFromBytes(buffer).Detected?.Encoding ?? Encoding.Default;

                s.Position = 0;
                viewer.Load(s);
            }

            //viewer.Load(path);
            viewer.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(path));
        }
    }
}