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

using System;
using System.Text;

namespace QuickLook.Plugin.TextViewer;

/// <summary>Validates JSON and inserts indentation without converting any token values.</summary>
internal sealed class JsonBeautifier
{
    private const int MaxDepth = 128;
    private const int MaxOutputLength = 20 * 1024 * 1024;
    private readonly string _source;
    private readonly StringBuilder _output = new();
    private int _position;

    private JsonBeautifier(string source) => _source = source;

    internal static bool TryFormat(string source, out string formatted)
    {
        formatted = null;
        if (source is null) return false;
        try
        {
            var parser = new JsonBeautifier(source);
            // EncodingDetector may leave a BOM in the decoded text.
            if (source.Length > 0 && source[0] == '\uFEFF') parser._position++;
            parser.Value(0);
            parser.WhiteSpace();
            if (parser._position != source.Length) return false;
            formatted = parser._output.ToString();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private void WhiteSpace()
    {
        while (_position < _source.Length && _source[_position] is ' ' or '\t' or '\r' or '\n')
            _position++;
    }

    private char Current => _position < _source.Length ? _source[_position] : '\0';

    private void Append(string value)
    {
        if (value.Length > MaxOutputLength - _output.Length) throw new FormatException();
        _output.Append(value);
    }

    private void NewLine(int depth)
    {
        Append(Environment.NewLine);
        Append(new string(' ', depth * 2));
    }

    private void Require(char c)
    {
        WhiteSpace();
        if (Current != c) throw new FormatException();
        _position++;
        Append(c.ToString());
    }

    private void Value(int depth)
    {
        WhiteSpace();
        switch (Current)
        {
            case '{': Container(depth, true); break;
            case '[': Container(depth, false); break;
            case '"': String(); break;
            case 't': Literal("true"); break;
            case 'f': Literal("false"); break;
            case 'n': Literal("null"); break;
            default: Number(); break;
        }
    }

    private void Container(int depth, bool isObject)
    {
        if (depth >= MaxDepth) throw new FormatException();
        char close = isObject ? '}' : ']';
        Require(isObject ? '{' : '[');
        WhiteSpace();
        if (Current == close) { Require(close); return; }
        while (true)
        {
            NewLine(depth + 1);
            if (isObject)
            {
                WhiteSpace();
                String();
                Require(':');
                Append(" ");
            }
            Value(depth + 1);
            WhiteSpace();
            if (Current == close) break;
            Require(',');
        }
        NewLine(depth);
        Require(close);
    }

    private void String()
    {
        if (Current != '"') throw new FormatException();
        int start = _position++;
        while (_position < _source.Length)
        {
            char c = _source[_position++];
            if (c == '"')
            {
                Append(_source.Substring(start, _position - start));
                return;
            }
            if (c < 0x20) throw new FormatException();
            if (c != '\\') continue;
            if (_position >= _source.Length) throw new FormatException();
            char escape = _source[_position++];
            if (escape == 'u')
            {
                for (int i = 0; i < 4; i++)
                {
                    char hex = Current;
                    if (!(hex is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
                        throw new FormatException();
                    _position++;
                }
            }
            else if (!(escape is '"' or '\\' or '/' or 'b' or 'f' or 'n' or 'r' or 't'))
                throw new FormatException();
        }
        throw new FormatException();
    }

    private void Literal(string literal)
    {
        if (_source.Length - _position < literal.Length ||
            string.CompareOrdinal(_source, _position, literal, 0, literal.Length) != 0)
            throw new FormatException();
        _position += literal.Length;
        Append(literal);
    }

    private bool IsDigit => Current is >= '0' and <= '9';

    private void Digits()
    {
        if (!IsDigit) throw new FormatException();
        while (IsDigit) _position++;
    }

    private void Number()
    {
        int start = _position;
        if (Current == '-') _position++;
        if (Current == '0') _position++;
        else Digits();
        if (Current == '.') { _position++; Digits(); }
        if (Current is 'e' or 'E')
        {
            _position++;
            if (Current is '+' or '-') _position++;
            Digits();
        }
        Append(_source.Substring(start, _position - start));
    }
}
