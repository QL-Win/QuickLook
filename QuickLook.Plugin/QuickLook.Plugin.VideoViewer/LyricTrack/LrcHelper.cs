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
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuickLook.Plugin.VideoViewer.LyricTrack;

/// <summary>
/// https://github.com/lemutec/LyricStudio
/// </summary>
public static class LrcHelper
{
    public static IEnumerable<LrcLine> ParseText(string text)
    {
        List<LrcLine> lrcList = [];

        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        string[] lines = new Regex(@"\r?\n").Split(text);

        // The text does not contain timecode
        if (!new Regex(@"\[\d+\:\d+\.\d+\]").IsMatch(text))
        {
            foreach (string line in lines)
            {
                if (new Regex(@"\[\w+\:.*\]").IsMatch(line))
                {
                    lrcList.Add(new LrcLine(null, line.Trim('[', ']')));
                }
                else
                {
                    lrcList.Add(new LrcLine(0, line));
                }
            }
        }
        // The text contain timecode
        else
        {
            bool multiLrc = false;

            int lineNumber = 1;
            try
            {
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        lineNumber++;
                        continue;
                    }

                    MatchCollection matches = new Regex(@"\[\d+\:\d+\.\d+\]").Matches(line);

                    // Such as [00:00.000][00:01.000]
                    if (matches.Count > 1)
                    {
                        string lrc = new Regex(@"(?<=\])[^\]]+$").Match(line).ToString();

                        lrcList.AddRange(matches.OfType<Match>().Select(match => new LrcLine(ParseTimeSpan(match.ToString().Trim('[', ']')), lrc)));
                        multiLrc = true;
                    }
                    // Normal line like [00:00.000]
                    else if (matches.Count == 1)
                    {
                        lrcList.Add(LrcLine.Parse(line));
                    }
                    // Info line
                    else if (new Regex(@"\[\w+\:.*\]").IsMatch(line))
                    {
                        lrcList.Add(new LrcLine(null, new Regex(@"\[\w+\:.*\]").Match(line).ToString().Trim('[', ']')));
                    }
                    // Not an empty line but no any timecode was found, so add an empty timecode
                    else
                    {
                        lrcList.Add(new LrcLine(TimeSpan.Zero, line));
                    }
                    lineNumber++;
                }
                // Multi timecode and sort it auto
                if (multiLrc)
                {
                    lrcList = [.. lrcList.OrderBy(x => x.LrcTime)];
                }
            }
            catch (Exception e)
            {
                // Some error occurred in {{ lineNumber }}
                Debug.WriteLine(e);
            }
        }

        return lrcList;
    }

    public static LrcLine GetNearestLrc(IEnumerable<LrcLine> lrcList, TimeSpan time)
    {
        LrcLine line = lrcList
            .Where(x => x.LrcTime != null && x.LrcTime <= time)
            .OrderByDescending(x => x.LrcTime)
            .FirstOrDefault();

        return line;
    }

    /// <summary>
    /// Try to resolve the timestamp string to TimeSpan, see
    /// <seealso cref="ParseTimeSpan(string)"/>
    /// </summary>
    public static bool TryParseTimeSpan(string s, out TimeSpan ts)
    {
        try
        {
            ts = ParseTimeSpan(s);
            return true;
        }
        catch
        {
            ts = TimeSpan.Zero;
            return false;
        }
    }

    /// <summary>
    /// Resolves the timestamp string to a TimeSpan
    /// </summary>
    public static TimeSpan ParseTimeSpan(string s)
    {
        // If the millisecond is two-digit, add an extra 0 at the end
        if (s.Split('.')[1].Length == 2)
        {
            s += '0';
        }
        return TimeSpan.Parse("00:" + s);
    }

    /// <summary>
    /// Change the timestamp to a two-digit millisecond format
    /// </summary>
    public static string ToShortString(this TimeSpan ts, bool isShort = false)
    {
        if (isShort)
        {
            return $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
        }
        else
        {
            return $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
        }
    }
}
