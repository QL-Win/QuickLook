// Copyright © 2017-2025 QL-Win Contributors
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

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.TextViewer.Detectors;
using QuickLook.Plugin.TextViewer.Themes;
using QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace QuickLook.Plugin.TextViewer;

public partial class TextViewerPanel : TextEditor, IDisposable
{
    private bool _disposed;

    public TextViewerPanel()
    {
        FontSize = 14;
        ShowLineNumbers = true;
        WordWrap = true;
        IsReadOnly = true;
        IsManipulationEnabled = true;
        Options.EnableEmailHyperlinks = false;
        Options.EnableHyperlinks = false;

        ContextMenu = new ContextMenu();
        ContextMenu.Items.Add(new MenuItem
        {
            Header = TranslationHelper.Get("Editor_Copy", domain: Assembly.GetExecutingAssembly().GetName().Name),
            Command = ApplicationCommands.Copy
        });
        ContextMenu.Items.Add(new MenuItem
        {
            Header = TranslationHelper.Get("Editor_SelectAll",
                domain: Assembly.GetExecutingAssembly().GetName().Name),
            Command = ApplicationCommands.SelectAll
        });

        ManipulationInertiaStarting += Viewer_ManipulationInertiaStarting;
        ManipulationStarting += Viewer_ManipulationStarting;
        ManipulationDelta += Viewer_ManipulationDelta;

        PreviewMouseWheel += Viewer_MouseWheel;

        FontFamily = new FontFamily(TranslationHelper.Get("Editor_FontFamily",
            domain: Assembly.GetExecutingAssembly().GetName().Name));

        TextArea.TextView.ElementGenerators.Add(new TruncateLongLines());

        SearchPanel.Install(this);
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
            DesiredDeceleration = 10d * 96d / (1000d * 1000d)
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

    private class TruncateLongLines : VisualLineElementGenerator
    {
        private const int MAX_LENGTH = 10000;
        private const string ELLIPSIS = "⁞⁞[TRUNCATED]⁞⁞";

        public override int GetFirstInterestedOffset(int startOffset)
        {
            var line = CurrentContext.VisualLine.LastDocumentLine;
            if (line.Length > MAX_LENGTH)
            {
                int ellipsisOffset = line.Offset + MAX_LENGTH - ELLIPSIS.Length;
                if (startOffset <= ellipsisOffset)
                    return ellipsisOffset;
            }
            return -1;
        }

        public override VisualLineElement ConstructElement(int offset)
        {
            return new FormattedTextElement(ELLIPSIS, CurrentContext.VisualLine.LastDocumentLine.EndOffset - offset);
        }
    }

    public void LoadFileAsync(string path, ContextObject context)
    {
        _ = Task.Run(() =>
        {
            const int maxLength = 5 * 1024 * 1024;
            const int maxHighlightingLength = (int)(0.5d * 1024 * 1024);
            var buffer = new MemoryStream();
            bool fileTooLong;

            using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileTooLong = s.Length > maxLength;
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

            if (fileTooLong)
                context.Title += " (0 ~ 5MB)";

            var bufferCopy = buffer.ToArray();
            buffer.Dispose();

            var encoding = EncodingDetector.DetectFromBytes(bufferCopy);
            var text = encoding.GetString(bufferCopy);
            var doc = new TextDocument(text);
            doc.SetOwnerThread(Dispatcher.Thread);

            if (_disposed)
                return;

            Dispatcher.BeginInvoke(() =>
            {
                var extension = Path.GetExtension(path);
                var highlighting = HighlightingThemeManager.GetHighlightingByExtensionOrDetector(extension, text);

                Encoding = encoding;
                SyntaxHighlighting = bufferCopy.Length > maxHighlightingLength
                    ? null
                    : highlighting.SyntaxHighlighting;
                Document = doc;

                if (SyntaxHighlighting is ICustomHighlightingDefinition custom)
                {
                    foreach (var lineTransformer in custom.LineTransformers)
                    {
                        TextArea.TextView.LineTransformers.Add(lineTransformer);
                    }
                }

                if (highlighting.IsDark)
                {
                    Background = Brushes.Transparent;
                    SetResourceReference(ForegroundProperty, "WindowTextForeground");
                }
                else
                {
                    // if os dark mode, but not AllowDarkTheme, make background light
                    Background = OSThemeHelper.AppsUseDarkTheme()
                        ? new SolidColorBrush(Color.FromArgb(175, 255, 255, 255))
                        : Brushes.Transparent;
                }

                if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    if (CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
                    {
                        string isSupportRTL = TranslationHelper.Get("IsSupportRTL",
                            failsafe: bool.TrueString,
                            domain: Assembly.GetExecutingAssembly().GetName().Name);

                        if (bool.TrueString.Equals(isSupportRTL, StringComparison.OrdinalIgnoreCase))
                            FlowDirection = System.Windows.FlowDirection.RightToLeft;
                    }
                }

                context.IsBusy = false;
            }, DispatcherPriority.Render);
        });
    }
}
