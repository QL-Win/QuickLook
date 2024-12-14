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
using System.Diagnostics;

namespace QuickLook.Plugin.VideoViewer.LyricTrack;

/// <summary>
/// https://github.com/lemutec/LyricStudio
/// https://en.wikipedia.org/wiki/LRC_(file_format)
/// </summary>
[DebuggerDisplay("{PreviewText}")]
public class LrcLine : IComparable<LrcLine>
{
    public static readonly LrcLine Empty = new();

    public TimeSpan? LrcTime { get; set; } = default;

    public static bool IsShort { get; set; } = false;

    public string LrcTimeText
    {
        get => LrcTime.HasValue ? LrcHelper.ToShortString(LrcTime.Value, IsShort) : string.Empty;
        set
        {
            if (LrcHelper.TryParseTimeSpan(value, out TimeSpan ts))
            {
                LrcTime = ts;
            }
            else
            {
                LrcTime = null;
            }
        }
    }

    public string LrcText { get; set; }

    /// <summary>
    /// Preview such as [{LrcTime:mm:ss.fff}]{LyricText}
    /// </summary>
    public string PreviewText
    {
        get
        {
            if (LrcTime.HasValue)
            {
                return $"[{LrcHelper.ToShortString(LrcTime.Value, IsShort)}]{LrcText}";
            }
            else if (!string.IsNullOrWhiteSpace(LrcText))
            {
                return $"[{LrcText}]";
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public LrcLine(double time, string text)
    {
        LrcTime = new TimeSpan(0, 0, 0, 0, (int)(time * 1000));
        LrcText = text;
    }

    public LrcLine(TimeSpan? time, string text)
    {
        LrcTime = time;
        LrcText = text;
    }

    public LrcLine(TimeSpan? time)
        : this(time, string.Empty)
    {
    }

    public LrcLine(LrcLine lrcLine)
    {
        LrcTime = lrcLine.LrcTime;
        LrcText = lrcLine.LrcText;
    }

    public LrcLine(string line)
        : this(Parse(line))
    {
    }

    public LrcLine()
    {
        LrcTime = null;
        LrcText = string.Empty;
    }

    public static LrcLine Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return Empty;
        }

        if (CheckMultiLine(line))
        {
            throw new FormatException();
        }

        string[] slices = line.TrimStart().TrimStart('[').Split(']');

        if (!LrcHelper.TryParseTimeSpan(slices[0], out TimeSpan time))
        {
            return new LrcLine(null, slices[0]);
        }

        return new LrcLine(time, slices[1]);
    }

    public static bool TryParse(string line, out LrcLine lrcLine)
    {
        try
        {
            lrcLine = Parse(line);
            return true;
        }
        catch
        {
            lrcLine = Empty;
            return false;
        }
    }

    public static bool CheckMultiLine(string line)
    {
        if (line.TrimStart().IndexOf('[', 1) != -1) return true;
        else return false;
    }

    public override string ToString() => PreviewText;

    public int CompareTo(LrcLine other)
    {
        // Sort order: null < TimeSpan < string
        if (!LrcTime.HasValue) return -1;
        if (!other.LrcTime.HasValue) return 1;
        return LrcTime.Value.CompareTo(other.LrcTime.Value);
    }
}
