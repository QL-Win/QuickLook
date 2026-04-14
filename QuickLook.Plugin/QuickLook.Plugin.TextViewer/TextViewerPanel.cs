// Copyright © 2017-2026 QL-Win Contributors
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
using ICSharpCode.AvalonEdit.Editing;
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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Encoding = System.Text.Encoding;

namespace QuickLook.Plugin.TextViewer;

public partial class TextViewerPanel : TextEditor, IDisposable
{
    private bool _disposed;

    /// <summary>Maximum number of characters allowed on a single line before it is truncated.</summary>
    private const int MAX_LINE_LENGTH = 10000;

    /// <summary>Marker appended at the end of a truncated line to indicate omitted content.</summary>
    private const string ELLIPSIS = "⁞⁞[TRUNCATED]⁞⁞";

    static TextViewerPanel()
    {
        // Implementation of the Search Panel Styled with Fluent Theme
        {
            var groupDictionary = new ResourceDictionary();
            groupDictionary.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/QuickLook.Plugin.TextViewer;component/Controls/DropDownButton.xaml", UriKind.Absolute)
            });
            groupDictionary.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("pack://application:,,,/QuickLook.Plugin.TextViewer;component/Controls/SearchPanel.xaml", UriKind.Absolute)
            });
            Application.Current.Resources.MergedDictionaries.Add(groupDictionary);
        }

        // Initialize the Highlighting Theme Manager
        HighlightingThemeManager.Initialize();
    }

    public TextViewerPanel()
    {
        FontSize = 14;
        ShowLineNumbers = true;
        WordWrap = true;
        IsReadOnly = true;

        // Enable manipulation events (touch gestures like pan/scroll).
        IsManipulationEnabled = true;

        // Disable automatic hyperlink detection for email addresses.
        Options.EnableEmailHyperlinks = false;

        // Disable automatic hyperlink detection for general URLs.
        Options.EnableHyperlinks = false;

        // Search for the separator line inside the left margins of the TextArea.
        // The default LineNumberMargin in AvalonEdit often contains a thin Line element
        // used as a visual separator between line numbers and the text.
        // If found, set its Stroke to Transparent to hide the separator visually.
        TextArea.LeftMargins
            .OfType<System.Windows.Shapes.Line>()
            .FirstOrDefault()
            ?.Stroke = Brushes.Transparent;

        ContextMenu = new ContextMenu();
        // Add "Copy" menu item.
        ContextMenu.Items.Add(new MenuItem
        {
            Header = TranslationHelper.Get("Editor_Copy", domain: Assembly.GetExecutingAssembly().GetName().Name),
            Command = ApplicationCommands.Copy
        });
        // Add "Select All" menu item.
        ContextMenu.Items.Add(new MenuItem
        {
            Header = TranslationHelper.Get("Editor_SelectAll",
                domain: Assembly.GetExecutingAssembly().GetName().Name),
            Command = ApplicationCommands.SelectAll
        });

        ManipulationInertiaStarting += Viewer_ManipulationInertiaStarting;
        ManipulationStarting += Viewer_ManipulationStarting;
        ManipulationDelta += Viewer_ManipulationDelta;
        KeyDown += Viewer_KeyDown;

        PreviewMouseWheel += Viewer_MouseWheel;

        FontFamily = new FontFamily(TranslationHelper.Get("Editor_FontFamily",
            domain: Assembly.GetExecutingAssembly().GetName().Name));

        // Add a custom element generator (e.g., to truncate extremely long lines).
        TextArea.TextView.ElementGenerators.Add(new TruncateLongLines());

        // Install the search panel (Ctrl+F style search UI).
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

    private void Viewer_KeyDown(object sender, KeyEventArgs e)
    {
        // Support keyboard shortcuts for RTL and LTR text direction
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
            // RTL: Ctrl + RShift
            // LTR: Ctrl + LShift
            if (Keyboard.IsKeyDown(Key.RightShift))
                FlowDirection = FlowDirection.RightToLeft;
            else if (Keyboard.IsKeyDown(Key.LeftShift))
                FlowDirection = FlowDirection.LeftToRight;
        }
        else if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
        {
            if (Keyboard.IsKeyDown(Key.Z))
            {
                WordWrap = !WordWrap;
            }
        }
    }

    /// <summary>
    /// Fallback visual-layer guard: if a line somehow reaches the renderer still over the limit,
    /// replace the tail with <see cref="ELLIPSIS"/> so AvalonEdit never tries to measure the full run.
    /// </summary>
    private class TruncateLongLines : VisualLineElementGenerator
    {
        public override int GetFirstInterestedOffset(int startOffset)
        {
            var line = CurrentContext.VisualLine.LastDocumentLine;
            if (line.Length > MAX_LINE_LENGTH)
            {
                // Position the insertion point so that the visible prefix + ELLIPSIS
                // exactly fills MAX_LINE_LENGTH characters.
                int ellipsisOffset = line.Offset + MAX_LINE_LENGTH - ELLIPSIS.Length;
                if (startOffset <= ellipsisOffset)
                    return ellipsisOffset;
            }
            return -1;
        }

        public override VisualLineElement ConstructElement(int offset)
        {
            // Consume every character from `offset` to end-of-line and replace
            // them with a single non-interactive text run showing ELLIPSIS.
            return new FormattedTextElement(ELLIPSIS, CurrentContext.VisualLine.LastDocumentLine.EndOffset - offset);
        }
    }

    /// <summary>
    /// Pre-processes the raw text on the background thread before handing it to AvalonEdit.
    /// Any line longer than <see cref="MAX_LINE_LENGTH"/> is hard-truncated: the excess characters are
    /// replaced with <see cref="ELLIPSIS"/> directly in the string, so the syntax highlighter and
    /// WPF word-wrap logic never see the original long content.
    /// </summary>
    /// <param name="text">The decoded file text. Modified in-place when truncation occurs.</param>
    /// <returns>
    /// <see langword="true"/> when at least one line was shortened; used to decide whether to
    /// install <see cref="TruncatedLineDecolorizer"/>.
    /// </returns>
    private static bool TruncateLongLinesInText(ref string text)
    {
        // Fast-path: if the whole text is shorter than the limit, no line can exceed it.
        if (text.Length <= MAX_LINE_LENGTH)
            return false;

        bool found = false;
        var sb = new System.Text.StringBuilder(text.Length);
        int lineStart = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\r' || c == '\n')
            {
                int lineLen = i - lineStart;
                if (lineLen > MAX_LINE_LENGTH)
                {
                    // Keep only the first (MAX_LINE_LENGTH - ELLIPSIS.Length) chars,
                    // then append the marker so the total stays at MAX_LINE_LENGTH.
                    sb.Append(text, lineStart, MAX_LINE_LENGTH - ELLIPSIS.Length);
                    sb.Append(ELLIPSIS);
                    found = true;
                }
                else
                {
                    sb.Append(text, lineStart, lineLen);
                }

                sb.Append(c);
                // Consume the LF that follows a CR so we don't double-count it.
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                    sb.Append('\n');
                }
                lineStart = i + 1;
            }
        }

        // Handle the last line when there is no trailing newline.
        if (lineStart < text.Length)
        {
            int lineLen = text.Length - lineStart;
            if (lineLen > MAX_LINE_LENGTH)
            {
                sb.Append(text, lineStart, MAX_LINE_LENGTH - ELLIPSIS.Length);
                sb.Append(ELLIPSIS);
                found = true;
            }
            else
            {
                sb.Append(text, lineStart, lineLen);
            }
        }

        if (found)
            text = sb.ToString();

        return found;
    }

    /// <summary>
    /// Runs after the syntax highlighter to strip all coloring from truncated lines.
    /// A line is considered truncated when its last characters match <see cref="ELLIPSIS"/>.
    /// Resetting the foreground to the editor's base color effectively removes
    /// any syntax-highlight spans, keeping the display clean and readable.
    /// </summary>
    private class TruncatedLineDecolorizer : DocumentColorizingTransformer
    {
        private readonly TextViewerPanel _owner;

        public TruncatedLineDecolorizer(TextViewerPanel owner) => _owner = owner;

        protected override void ColorizeLine(DocumentLine line)
        {
            // Skip lines that are too short to contain the marker.
            if (line.Length < ELLIPSIS.Length)
                return;

            // Check whether the line ends with the truncation marker.
            int markerStart = line.EndOffset - ELLIPSIS.Length;
            if (CurrentContext.Document.GetText(markerStart, ELLIPSIS.Length) == ELLIPSIS)
            {
                // Override the entire line's foreground with the editor default,
                // which removes any previously applied syntax-highlight colors.
                ChangeLinePart(line.Offset, line.EndOffset, element =>
                {
                    element.TextRunProperties.SetForegroundBrush(_owner.Foreground);
                });
            }
        }
    }

    public void LoadFileAsync(string path, ContextObject context)
    {
        _ = Task.Run(() =>
        {
            const int maxLength = 5 * 1024 * 1024;
            const int maxHighlightingLength = (int)(0.5d * 1024 * 1024);
            var buffer = new MemoryStream();
            bool fileTooLong = false;

            // Read file to memory stream
            if (FormatDetector.Transfer(path, out string transferred))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(transferred);
                buffer.Write(bytes, 0, bytes.Length);
            }
            else
            {
                using var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

            // Truncate overly long lines to prevent crashes and lag
            bool hasLongLines = TruncateLongLinesInText(ref text);

            var doc = new TextDocument(text);
            doc.SetOwnerThread(Dispatcher.Thread);

            if (_disposed)
                return;

            Dispatcher.BeginInvoke(() =>
            {
                var extension = Path.GetExtension(path);
                var highlighting = HighlightingThemeManager.GetHighlightingByExtensionOrDetector(path, extension, text);

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

                // Only install the decolorizer when the file actually contained long lines,
                // to avoid the per-line overhead on normal files.
                if (hasLongLines)
                {
                    TextArea.TextView.LineTransformers.Add(new TruncatedLineDecolorizer(this));
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

                // Support automatic RTL for text files
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
