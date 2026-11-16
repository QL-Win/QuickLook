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

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using QuickLook.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;

namespace QuickLook.Plugin.TextViewer.Generators;

/// <summary>
/// "Privacy mode" for .env files: replaces the value of every KEY=value line with a fixed-width
/// mask on screen, so secrets are not exposed during screen shares or presentations.
/// The document itself is not modified, and the mask has a constant width so it does not reveal
/// how long a value is.
/// </summary>
/// <remarks>
/// The generator must be built from the decoded text BEFORE over-long lines are truncated.
/// Truncation can cut off the closing quote of a value, and a value whose end is not visible
/// cannot be masked reliably. Masked ranges are stored as (line, column) instead of offsets:
/// truncation only shortens the end of lines and keeps every line break, so line and column
/// stay valid, and a range is clipped to the real line length when it is drawn.
/// </remarks>
internal sealed class EnvMaskGenerator : VisualLineElementGenerator
{
    private const string Mask = "********";

    private static readonly char[] LineBreaks = ['\r', '\n'];

    // One entry per masked line, sorted by line number. A line has at most one masked range, and a
    // quoted value that spans several lines becomes one range per line.
    // Line numbers are 1-based like DocumentLine.LineNumber, columns are 0-based [start, end).
    private readonly int[] _lines;
    private readonly int[] _startColumns;
    private readonly int[] _endColumns;

    /// <summary>When false the real values are shown.</summary>
    public bool IsMasked { get; set; } = true;

    /// <param name="text">The decoded file text, before any line truncation.</param>
    public EnvMaskGenerator(string text)
    {
        var lines = new List<int>();
        var starts = new List<int>();
        var ends = new List<int>();
        FindValueRanges(text ?? string.Empty, lines, starts, ends);

        _lines = lines.ToArray();
        _startColumns = starts.ToArray();
        _endColumns = ends.ToArray();
    }

    /// <summary>
    /// .env, .env.local, .env.production, .envrc, something.env ...
    /// </summary>
    public static bool IsEnvFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        string name = Path.GetFileName(path);

        return name.Equals(".env", StringComparison.OrdinalIgnoreCase)
               || name.Equals(".envrc", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith(".env.", StringComparison.OrdinalIgnoreCase)
               || name.EndsWith(".env", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Masking applies to .env files unless the user switched it off with the
    /// "MaskEnvValues" setting of the text viewer.
    /// </summary>
    public static bool IsEnabledFor(string path)
    {
        return IsEnvFile(path) && SettingHelper.Get("MaskEnvValues", true, "QuickLook.Plugin.TextViewer");
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        if (!IsMasked || _lines.Length == 0)
            return -1;

        // A visual line normally is one document line, but can be several when lines are folded.
        int limit = CurrentContext.VisualLine.LastDocumentLine.EndOffset;

        for (DocumentLine line = CurrentContext.Document.GetLineByOffset(startOffset);
             line != null && line.Offset <= limit;
             line = line.NextLine)
        {
            if (TryGetRange(line, out int start, out int end) && end > startOffset)
                return Math.Max(start, startOffset);
        }

        return -1;
    }

    public override VisualLineElement ConstructElement(int offset)
    {
        DocumentLine line = CurrentContext.Document.GetLineByOffset(offset);

        if (!TryGetRange(line, out int start, out int end) || offset < start || offset >= end)
            return null;

        return new FormattedTextElement(Mask, end - offset);
    }

    /// <summary>
    /// Document offsets of the masked range on a line, clipped to the text that is really there
    /// (a truncated line is shorter than the line the range was found on).
    /// </summary>
    private bool TryGetRange(DocumentLine line, out int start, out int end)
    {
        start = end = 0;

        int index = Array.BinarySearch(_lines, line.LineNumber);
        if (index < 0)
            return false;

        start = line.Offset + _startColumns[index];
        end = line.Offset + Math.Min(_endColumns[index], line.Length);
        return end > start;
    }

    private static void FindValueRanges(string text, List<int> lines, List<int> starts, List<int> ends)
    {
        int position = 0;
        int lineNumber = 1;

        while (position < text.Length)
        {
            int lineEnd = IndexOfLineBreak(text, position);
            int nextLine = SkipLineBreak(text, lineEnd);

            // A UTF-8 BOM is decoded to U+FEFF. It stays part of the text (so columns match the
            // document) but is not part of the first key.
            int p = position == 0 && text[0] == '\uFEFF' ? 1 : position;
            p = SkipBlanks(text, p, lineEnd);

            // Blank lines and comments have nothing to mask.
            if (p < lineEnd && text[p] != '#')
            {
                p = SkipExport(text, p, lineEnd);

                int keyStart = p;
                while (p < lineEnd && IsKeyChar(text[p]))
                    p++;

                if (p > keyStart)
                {
                    p = SkipBlanks(text, p, lineEnd);

                    if (p < lineEnd && text[p] == '=')
                    {
                        int valueStart = SkipBlanks(text, p + 1, lineEnd);

                        // KEY= (empty value): nothing to hide.
                        if (valueStart < lineEnd)
                        {
                            int valueEnd = ScanValue(text, valueStart);
                            AddRanges(text, valueStart, valueEnd, lineNumber, position, lines, starts, ends);

                            // Resume after the line that holds the end of the value.
                            nextLine = SkipLineBreak(text, IndexOfLineBreak(text, valueEnd));
                        }
                    }
                }
            }

            lineNumber += CountLineBreaks(text, position, nextLine);
            position = nextLine;
        }
    }

    /// <summary>
    /// Returns the end (exclusive) of the value that starts at <paramref name="start"/>.
    /// A value is a sequence of quoted and unquoted segments, like a shell word (see "Quoting" in
    /// the POSIX Shell Command Language): <c>"prefix"secret</c> is one value, and a backslash
    /// escapes the next character, so an escaped space does not end the value or start a comment, and
    /// an escaped line break continues the value on the next line.
    /// It ends at the end of the line, or at unescaped whitespace followed by '#' (a comment).
    /// To be safe, anything that may belong to the value is masked.
    /// </summary>
    private static int ScanValue(string text, int start)
    {
        int i = start;
        int end = start;

        // True at the start of the value and right after a closing quote.
        bool afterQuote = true;

        // End of the line that holds the scan position, and the start of the next assignment after
        // it. Both are found on first use and reused while the scan stays on that line, so a line
        // with many quotes is not rescanned for each of them.
        int lineEnd = -1;
        int nextAssignment = -1;

        while (i < text.Length)
        {
            char c = text[i];

            if (c == '\r' || c == '\n')
                break;

            if (c == '"' || c == '\'' || c == '`')
            {
                // A quoted segment may continue over several lines (private keys, certificates...),
                // also after an unquoted prefix: TOKEN=prefix"first\nsecret". In the middle of a
                // word, though, a quote is usually just a character (NAME=O'Brien), so it must not
                // pair with a quote that belongs to the next assignment: the search stops there.
                int limit = text.Length;

                if (!afterQuote)
                {
                    if (i > lineEnd)
                    {
                        lineEnd = IndexOfLineBreak(text, i);
                        nextAssignment = -1;
                    }

                    if (nextAssignment < 0)
                        nextAssignment = IndexOfNextAssignment(text, lineEnd);

                    limit = nextAssignment;
                }

                int close = FindClosingQuote(text, i + 1, c, limit);

                if (close >= 0)
                {
                    i = close + 1;
                    end = i;
                    afterQuote = true;
                    continue;
                }

                // Never closed. At the start of a value hide the rest of the line, otherwise
                // treat the quote as an ordinary character.
                if (afterQuote)
                    return IndexOfLineBreak(text, i);

                i++;
                end = i;
                continue;
            }

            afterQuote = false;

            if (c == '\\')
            {
                // The escaped character (even a space) belongs to the value.
                // A backslash before a line break continues the value on the next line.
                if (i + 1 < text.Length && text[i + 1] == '\r' && i + 2 < text.Length && text[i + 2] == '\n')
                    i += 3;
                else
                    i += i + 1 < text.Length ? 2 : 1;

                end = i;
            }
            else if (c == ' ' || c == '\t')
            {
                int next = SkipBlanks(text, i, text.Length);

                // Unescaped whitespace followed by '#' starts a comment.
                if (next < text.Length && text[next] == '#')
                    break;

                // Whitespace inside the value; it only counts when more of the value follows.
                i = next;
            }
            else
            {
                i++;
                end = i;
            }
        }

        return end;
    }

    /// <summary>
    /// Records [start, end) split at line breaks, so that no range crosses a line.
    /// <paramref name="lineNumber"/> and <paramref name="lineStart"/> belong to the line that holds <paramref name="start"/>.
    /// </summary>
    private static void AddRanges(string text, int start, int end, int lineNumber, int lineStart,
        List<int> lines, List<int> starts, List<int> ends)
    {
        int segment = start;

        for (int i = start; i < end; i++)
        {
            char c = text[i];
            if (c != '\r' && c != '\n')
                continue;

            if (i > segment)
            {
                lines.Add(lineNumber);
                starts.Add(segment - lineStart);
                ends.Add(i - lineStart);
            }

            if (c == '\r' && i + 1 < end && text[i + 1] == '\n')
                i++;

            lineNumber++;
            lineStart = i + 1;
            segment = lineStart;
        }

        if (end > segment)
        {
            lines.Add(lineNumber);
            starts.Add(segment - lineStart);
            ends.Add(end - lineStart);
        }
    }

    /// <summary>Start of the first line after the line that ends at <paramref name="currentLineEnd"/> and begins a KEY=value assignment, or the end of the text.</summary>
    private static int IndexOfNextAssignment(string text, int currentLineEnd)
    {
        int position = SkipLineBreak(text, currentLineEnd);

        while (position < text.Length)
        {
            int lineEnd = IndexOfLineBreak(text, position);
            int p = SkipBlanks(text, position, lineEnd);

            if (p < lineEnd && text[p] != '#')
            {
                p = SkipExport(text, p, lineEnd);

                int keyStart = p;
                while (p < lineEnd && IsKeyChar(text[p]))
                    p++;

                if (p > keyStart)
                {
                    p = SkipBlanks(text, p, lineEnd);

                    if (p < lineEnd && text[p] == '=')
                        return position;
                }
            }

            position = SkipLineBreak(text, lineEnd);
        }

        return text.Length;
    }

    /// <summary>Index of the closing quote before <paramref name="limit"/>, or -1.</summary>
    private static int FindClosingQuote(string text, int from, char quote, int limit)
    {
        for (int i = from; i < limit; i++)
        {
            // Double quoted values may contain \" escapes.
            if (quote == '"' && text[i] == '\\')
            {
                i++;
                continue;
            }

            if (text[i] == quote)
                return i;
        }

        return -1;
    }

    /// <summary>Number of line breaks in [from, to); "\r\n" counts once, like in the editor.</summary>
    private static int CountLineBreaks(string text, int from, int to)
    {
        int count = 0;

        for (int i = from; i < to; i++)
        {
            if (text[i] == '\r')
            {
                if (i + 1 < to && text[i + 1] == '\n')
                    i++;

                count++;
            }
            else if (text[i] == '\n')
            {
                count++;
            }
        }

        return count;
    }

    private static int IndexOfLineBreak(string text, int from)
    {
        int index = text.IndexOfAny(LineBreaks, from);
        return index < 0 ? text.Length : index;
    }

    private static int SkipLineBreak(string text, int index)
    {
        if (index >= text.Length)
            return text.Length;

        if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
            return index + 2;

        return index + 1;
    }

    private static int SkipBlanks(string text, int from, int limit)
    {
        while (from < limit && (text[from] == ' ' || text[from] == '\t'))
            from++;

        return from;
    }

    /// <summary>Skips a leading "export " so that "export KEY=value" (.envrc, shell style) works too.</summary>
    private static int SkipExport(string text, int p, int lineEnd)
    {
        const string keyword = "export";

        if (p + keyword.Length < lineEnd
            && string.CompareOrdinal(text, p, keyword, 0, keyword.Length) == 0
            && (text[p + keyword.Length] == ' ' || text[p + keyword.Length] == '\t'))
        {
            return SkipBlanks(text, p + keyword.Length, lineEnd);
        }

        return p;
    }

    private static bool IsKeyChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '-';
    }
}