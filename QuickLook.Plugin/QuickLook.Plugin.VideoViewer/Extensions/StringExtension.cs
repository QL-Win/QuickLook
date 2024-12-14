// Copyright © 2024 ema
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
using System.Collections.Generic;

namespace QuickLook.Plugin.VideoViewer.Extensions;

internal static class StringExtension
{
    /// <summary>
    /// Splits a ReadOnlySpan<char> into an array of strings using the specified separator.
    /// </summary>
    /// <param name="input">The input ReadOnlySpan<char> to split.</param>
    /// <param name="separator">The ReadOnlySpan<char> separator to use for splitting.</param>
    /// <returns>An array of strings that are the result of splitting the input span.</returns>
    /// <remarks>
    /// - If the separator is not found, the entire input span will be returned as a single element.
    /// - If the input is empty, the method will return an empty array.
    /// - This method avoids allocating intermediate substrings during processing.
    /// </remarks>
    public static string[] Split(this string input, string separator)
    {
        if (input == null)
        {
            return [input];
        }

        ReadOnlySpan<char> @in = input.AsSpan();
        ReadOnlySpan<char> sep = separator.AsSpan();

        List<string> result = [];
        int start = 0;

        // Continue splitting until no separator is found
        while (true)
        {
            // Find the next occurrence of the separator
            int index = @in.Slice(start).IndexOf(sep);
            if (index == -1)
            {
                // No more separators; add the remaining substring
                result.Add(@in.Slice(start).ToString());
                break;
            }

            // Add the substring before the separator to the result list
            result.Add(@in.Slice(start, index).ToString());

            // Move the start position past the separator
            start += index + sep.Length;
        }

        return [.. result];
    }
}
