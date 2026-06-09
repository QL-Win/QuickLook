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
using System.IO;

namespace QuickLook.Plugin.TextViewer.Detectors;

/// <summary>
/// Resolve conflicting file extension names between Prolog and Perl.
/// </summary>
public sealed class PrologDetector : IConfusedFormatDetector
{
    public string Name => "Prolog";

    public string Extension => ".pl";

    public bool Detect(string path, string text) =>
        PlFormatHelper.IsPlFile(path) && PlFormatHelper.LooksLikeProlog(text);
}

/// <summary>
/// Disambiguates <c>.pl</c> files between Prolog and Perl syntax highlighting.
/// </summary>
internal static class PlFormatHelper
{
    private const int SampleLength = 8192;

    internal static bool IsPlFile(string path) =>
        Path.GetExtension(path).Equals(".pl", StringComparison.OrdinalIgnoreCase);

    internal static bool LooksLikeProlog(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        ReadOnlySpan<char> span = text.AsSpan();
        span = SkipBom(span);

        if (TryReadShebang(span, out ReadOnlySpan<char> shebang))
        {
            if (ContainsIgnoreCase(shebang, "perl"))
                return false;

            if (ContainsIgnoreCase(shebang, "swipl")
                || ContainsIgnoreCase(shebang, "prolog")
                || ContainsIgnoreCase(shebang, "sicstus")
                || ContainsIgnoreCase(shebang, "yap"))
                return true;
        }

        if (span.Length > SampleLength)
            span = span.Slice(0, SampleLength);

        int prologScore = 0;
        int perlScore = 0;

        if (span.Contains(":-".AsSpan(), StringComparison.Ordinal))
            prologScore += 3;

        if (span.Contains("?-".AsSpan(), StringComparison.Ordinal))
            prologScore += 2;

        if (ContainsIgnoreCase(span, ":- module"))
            prologScore += 3;

        if (ContainsIgnoreCase(span, ":- use_module"))
            prologScore += 3;

        if (ContainsIgnoreCase(span, ":- dynamic"))
            prologScore += 2;

        if (ContainsIgnoreCase(span, ":- multifile"))
            prologScore += 2;

        if (ContainsIgnoreCase(span, ":- initialization"))
            prologScore += 2;

        if (ContainsIgnoreCase(span, "use strict"))
            perlScore += 3;

        if (ContainsIgnoreCase(span, "use warnings"))
            perlScore += 2;

        if (ContainsIgnoreCase(span, "use v5"))
            perlScore += 2;

        if (ContainsIgnoreCase(span, "sub "))
            perlScore += 1;

        if (ContainsIgnoreCase(span, "my $"))
            perlScore += 2;

        if (ContainsIgnoreCase(span, "our $"))
            perlScore += 1;

        if (ContainsIgnoreCase(span, "package "))
            perlScore += 2;

        if (ContainsIgnoreCase(span, "=pod"))
            perlScore += 2;

        if (ContainsIgnoreCase(span, "__DATA__"))
            perlScore += 2;

        if (ContainsIgnoreCase(span, "__END__"))
            perlScore += 2;

        foreach (ReadOnlySpan<char> line in new LineEnumerator(span))
        {
            ReadOnlySpan<char> trimmed = TrimStart(line);
            if (trimmed.IsEmpty)
                continue;

            if (trimmed[0] == '%')
                prologScore++;

            if (trimmed[0] == '#')
                perlScore++;
        }

        if (prologScore > perlScore)
            return true;

        if (perlScore > prologScore)
            return false;

        return false;
    }

    private static ReadOnlySpan<char> SkipBom(ReadOnlySpan<char> span)
    {
        if (span.Length > 0 && span[0] == '\uFEFF')
            return span.Slice(1);

        return span;
    }

    private static bool TryReadShebang(ReadOnlySpan<char> span, out ReadOnlySpan<char> shebang)
    {
        shebang = default;

        if (span.Length < 2 || span[0] != '#' || span[1] != '!')
            return false;

        int end = 0;
        while (end < span.Length && span[end] != '\n' && span[end] != '\r')
            end++;

        shebang = span.Slice(0, end);
        return true;
    }

    private static ReadOnlySpan<char> TrimStart(ReadOnlySpan<char> span)
    {
        int i = 0;
        while (i < span.Length && (span[i] == ' ' || span[i] == '\t'))
            i++;

        return span.Slice(i);
    }

    private static bool ContainsIgnoreCase(ReadOnlySpan<char> haystack, string needle)
    {
        return haystack.Contains(needle.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    private ref struct LineEnumerator
    {
        private ReadOnlySpan<char> _remaining;
        private ReadOnlySpan<char> _current;

        public LineEnumerator(ReadOnlySpan<char> span)
        {
            _remaining = span;
            _current = default;
        }

        public readonly ReadOnlySpan<char> Current => _current;

        public readonly LineEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_remaining.Length == 0)
                return false;

            for (int i = 0; i < _remaining.Length; i++)
            {
                char c = _remaining[i];

                if (c != '\r' && c != '\n')
                    continue;

                _current = _remaining.Slice(0, i);

                int nextStart = i + 1;

                if (c == '\r'
                    && nextStart < _remaining.Length
                    && _remaining[nextStart] == '\n')
                {
                    nextStart++;
                }

                _remaining = _remaining.Slice(nextStart);

                return true;
            }

            _current = _remaining;
            _remaining = default;

            return true;
        }
    }
}
