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
using System.Collections.Generic;

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions;

/// <summary>
/// Per-line JSON tokenizer for JSONL (JSON Lines) files.
/// Each call to ColorizeLine processes one independent JSON object/array.
/// </summary>
public class JsonLineHighlighter : DocumentColorizingTransformer
{
    private readonly string _keyColor;
    private readonly string _stringColor;
    private readonly string _numberColor;
    private readonly string _boolNullColor;
    private readonly string _braceColor;
    private readonly string _bracketColor;

    public JsonLineHighlighter(
        string keyColor,
        string stringColor,
        string numberColor,
        string boolNullColor,
        string braceColor,
        string bracketColor)
    {
        _keyColor = keyColor;
        _stringColor = stringColor;
        _numberColor = numberColor;
        _boolNullColor = boolNullColor;
        _braceColor = braceColor;
        _bracketColor = bracketColor;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        var text = CurrentContext.Document.GetText(line);
        if (string.IsNullOrWhiteSpace(text))
            return;

        ColorizeJsonLine(line, text);
    }

    private void ColorizeJsonLine(DocumentLine line, string text)
    {
        // Stack: true = currently inside an object (next unquoted string is a key),
        //        false = currently inside an array (all strings are values).
        var contextStack = new Stack<bool>();
        bool expectKey = false;

        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            switch (c)
            {
                case '{':
                    Colorize(line, i, i + 1, _braceColor);
                    contextStack.Push(true);
                    expectKey = true;
                    i++;
                    break;

                case '}':
                    Colorize(line, i, i + 1, _braceColor);
                    if (contextStack.Count > 0) contextStack.Pop();
                    expectKey = false;
                    i++;
                    break;

                case '[':
                    Colorize(line, i, i + 1, _bracketColor);
                    contextStack.Push(false);
                    expectKey = false;
                    i++;
                    break;

                case ']':
                    Colorize(line, i, i + 1, _bracketColor);
                    if (contextStack.Count > 0) contextStack.Pop();
                    expectKey = false;
                    i++;
                    break;

                case ':':
                    // Colon separates key from value; next token is a value.
                    expectKey = false;
                    i++;
                    break;

                case ',':
                    // In object context the next string is a key; in array it is a value.
                    expectKey = contextStack.Count > 0 && contextStack.Peek();
                    i++;
                    break;

                case '"':
                case '\'':
                    i = TokenizeString(line, text, i, c, expectKey);
                    if (expectKey)
                        expectKey = false; // key consumed, colon follows next
                    break;

                default:
                    if (c == '-' || char.IsDigit(c))
                    {
                        i = TokenizeNumber(line, text, i);
                        expectKey = false;
                    }
                    else if (char.IsLetter(c))
                    {
                        i = TokenizeKeyword(line, text, i);
                        expectKey = false;
                    }
                    else
                    {
                        i++;
                    }
                    break;
            }
        }
    }

    private int TokenizeString(DocumentLine line, string text, int start, char quote, bool isKey)
    {
        int i = start + 1; // skip opening quote
        while (i < text.Length)
        {
            if (text[i] == '\\')
            {
                i += 2; // skip escape sequence
                continue;
            }
            if (text[i] == quote)
            {
                i++; // include closing quote
                break;
            }
            i++;
        }
        Colorize(line, start, i, isKey ? _keyColor : _stringColor);
        return i;
    }

    private int TokenizeNumber(DocumentLine line, string text, int start)
    {
        int i = start;
        if (i < text.Length && text[i] == '-') i++; // optional leading minus
        while (i < text.Length && char.IsDigit(text[i])) i++;
        if (i < text.Length && text[i] == '.')
        {
            i++;
            while (i < text.Length && char.IsDigit(text[i])) i++;
        }
        if (i < text.Length && (text[i] == 'e' || text[i] == 'E'))
        {
            i++;
            if (i < text.Length && (text[i] == '+' || text[i] == '-')) i++;
            while (i < text.Length && char.IsDigit(text[i])) i++;
        }
        Colorize(line, start, i, _numberColor);
        return i;
    }

    private int TokenizeKeyword(DocumentLine line, string text, int start)
    {
        int i = start;
        while (i < text.Length && char.IsLetter(text[i])) i++;
        string keyword = text.Substring(start, i - start);
        if (keyword is "true" or "false" or "null")
            Colorize(line, start, i, _boolNullColor);
        return i;
    }

    private void Colorize(DocumentLine line, int from, int to, string hexColor)
    {
        ChangeLinePart(line.Offset + from, line.Offset + to, el =>
        {
            el.TextRunProperties.SetForegroundBrush(hexColor.ToBrush());
        });
    }
}
