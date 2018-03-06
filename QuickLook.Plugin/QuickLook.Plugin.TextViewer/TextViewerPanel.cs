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

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using UtfUnknown;

namespace QuickLook.Plugin.TextViewer
{
    public class TextViewerPanel : TextEditor, IDisposable
    {
        private readonly ContextObject _context;
        private bool _disposed;

        public TextViewerPanel(string path, ContextObject context)
        {
            _context = context;

            Background = Brushes.Transparent;
            FontSize = 14;
            ShowLineNumbers = true;
            WordWrap = true;
            IsReadOnly = true;
            IsManipulationEnabled = true;

            ManipulationInertiaStarting += Viewer_ManipulationInertiaStarting;
            ManipulationStarting += Viewer_ManipulationStarting;
            ManipulationDelta += Viewer_ManipulationDelta;

            PreviewMouseWheel += Viewer_MouseWheel;

            FontFamily = new FontFamily(TranslationHelper.Get("Editor_FontFamily"));

            LoadFileAsync(path);
        }

        public void Dispose()
        {
            _disposed = true;
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

            ScrollToVerticalOffset(VerticalOffset - e.Delta);
        }

        private void Viewer_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            e.Handled = true;

            var delta = e.DeltaManipulation;
            ScrollToVerticalOffset(VerticalOffset - delta.Translation.Y);
        }

        private void Viewer_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.Mode = ManipulationModes.Translate;
        }

        private void LoadFileAsync(string path)
        {
            Task.Run(() =>
            {
                const int maxLength = 50 * 1024 * 1024;
                var buffer = new MemoryStream();
                bool tooLong;

                using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    tooLong = s.Length > maxLength;
                    while (s.Position < s.Length && buffer.Length < maxLength)
                    {
                        if (_disposed)
                            break;

                        var lb = new byte[8192];
                        var count = s.Read(lb, 0, lb.Length);
                        buffer.Write(lb, 0, count);
                    }
                }

                if (_disposed)
                    return;

                if (tooLong)
                    _context.Title += " (0 ~ 50MB)";

                var bufferCopy = buffer.ToArray();
                buffer.Dispose();

                var encoding = CharsetDetector.DetectFromBytes(bufferCopy).Detected?.Encoding ??
                               Encoding.Default;

                var doc = new TextDocument(encoding.GetString(bufferCopy));
                doc.SetOwnerThread(Dispatcher.Thread);

                if (_disposed)
                    return;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Encoding = encoding;
                    SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(path));
                    Document = doc;

                    _context.IsBusy = false;
                }), DispatcherPriority.Render);
            });
        }
    }
}