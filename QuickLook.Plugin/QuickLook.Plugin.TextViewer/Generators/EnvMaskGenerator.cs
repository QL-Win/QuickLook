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
internal sealed class EnvMaskGenerator : VisualLineElementGenerator
{
    private const string Mask = "********";

    private static readonly char[] LineBreaks = ['\r', '\n'];

    // Sorted, non-overlapping [start, end) document ranges. Every range lies on a single line,
    // so a quoted value that spans several lines becomes one range per line.
    private readonly int[] _starts;
    private readonly int[] _ends;

    /// <summary>When false the real values are shown.</summary>
    public bool IsMasked { get; set; } = true;

    /// <param name="text">The exact text that is shown in the editor (offsets must match the document).</param>
    public EnvMaskGenerator(string text)
    {
        var starts = new List<int>();
        var ends = new List<int>();
        FindValueRanges(text ?? string.Empty, starts, ends);
        _starts = starts.ToArray();
        _ends = ends.ToArray();
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
        if (!IsMasked)
            return -1;

        int index = FirstRangeEndingAfter(startOffset);
        if (index < 0)
            return -1;

        int offset = Math.Max(_starts[index], startOffset);

        // Only react to ranges on the line that is being built.
        return offset < CurrentContext.VisualLine.LastDocumentLine.EndOffset ? offset : -1;
    }

    public override VisualLineElement ConstructElement(int offset)
    {
        int index = FirstRangeEndingAfter(offset);
        if (index < 0 || _starts[index] > offset)
            return null;

        return new FormattedTextElement(Mask, _ends[index] - offset);
    }

    /// <summary>Index of the first range whose end lies after <paramref name="offset"/>, or -1.</summary>
    private int FirstRangeEndingAfter(int offset)
    {
        int low = 0;
        int high = _ends.Length;

        while (low < high)
        {
            int mid = (low + high) >> 1;
            if (_ends[mid] > offset)
                high = mid;
            else
                low = mid + 1;
        }

        return low < _ends.Length ? low : -1;
    }

    private static void FindValueRanges(string text, List<int> starts, List<int> ends)
    {
        int position = 0;

        while (position < text.Length)
        {
            int lineEnd = IndexOfLineBreak(text, position);
            int nextLine = SkipLineBreak(text, lineEnd);

            int p = SkipBlanks(text, position, lineEnd);

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
                        nextLine = AddValueRanges(text, valueStart, lineEnd, nextLine, starts, ends);
                    }
                }
            }

            position = nextLine;
        }
    }

    /// <summary>
    /// Records the range(s) of one value and returns the offset where parsing should continue.
    /// </summary>
    private static int AddValueRanges(string text, int valueStart, int lineEnd, int nextLine,
        List<int> starts, List<int> ends)
    {
        // KEY= (empty value): nothing to hide.
        if (valueStart >= lineEnd)
            return nextLine;

        char first = text[valueStart];

        if (first == '"' || first == '\'' || first == '`')
        {
            int close = FindClosingQuote(text, valueStart + 1, first);

            if (close >= 0)
            {
                // The quoted value may span several lines (private keys, certificates...).
                AddRange(text, valueStart, close + 1, starts, ends);

                // Resume after the line that holds the closing quote.
                return SkipLineBreak(text, IndexOfLineBreak(text, close));
            }

            // No closing quote anywhere: hide the rest of this line, keep parsing the next one.
            AddRange(text, valueStart, TrimEnd(text, valueStart, lineEnd), starts, ends);
            return nextLine;
        }

        // Unquoted value: ends at the line end or at an inline comment (whitespace followed by '#').
        int end = lineEnd;
        for (int i = valueStart + 1; i < lineEnd; i++)
        {
            if (text[i] == '#' && (text[i - 1] == ' ' || text[i - 1] == '\t'))
            {
                end = i;
                break;
            }
        }

        end = TrimEnd(text, valueStart, end);
        if (end > valueStart)
            AddRange(text, valueStart, end, starts, ends);

        return nextLine;
    }

    /// <summary>Adds [start, end) split at line breaks, so that no range crosses a line.</summary>
    private static void AddRange(string text, int start, int end, List<int> starts, List<int> ends)
    {
        int segmentStart = start;

        for (int i = start; i < end; i++)
        {
            if (text[i] != '\r' && text[i] != '\n')
                continue;

            if (i > segmentStart)
            {
                starts.Add(segmentStart);
                ends.Add(i);
            }

            segmentStart = i + 1;
        }

        if (end > segmentStart)
        {
            starts.Add(segmentStart);
            ends.Add(end);
        }
    }

    private static int FindClosingQuote(string text, int from, char quote)
    {
        for (int i = from; i < text.Length; i++)
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

    private static int TrimEnd(string text, int start, int end)
    {
        while (end > start && (text[end - 1] == ' ' || text[end - 1] == '\t'))
            end--;

        return end;
    }

    private static bool IsKeyChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '-';
    }
}
