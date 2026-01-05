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

using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using UtfUnknown;

namespace QuickLook.Plugin.TextViewer.Detectors;

[Export]
public class EncodingDetector
{
    public static Encoding DetectFromBytes(byte[] bytes)
    {
        var result = CharsetDetector.DetectFromBytes(bytes);
        var encoding = result.DoubleDetectFromResult(bytes); // Fix issues

        return encoding;
    }
}

file static class DetectionExtensions
{
    public static Encoding DoubleDetectFromResult(this DetectionResult result, byte[] buffer)
    {
        // Determine the highest confidence encoding, or fallback to ANSI
        var encoding = result.Detected?.Encoding ?? Encoding.Default;

        // When mixing encodings, one of the encodings may gain higher confidence
        // In this case, we should return to encodings UTF8 / UTF32 / ANSI
        // https://github.com/QL-Win/QuickLook/issues/769
        if (encoding != Encoding.UTF8 && encoding != Encoding.UTF32 && encoding != Encoding.Default)
        {
            if (result.Details.Any(detail => detail.Encoding == Encoding.UTF8))
            {
                encoding = Encoding.UTF8;
            }
            else if (result.Details.Any(detail => detail.Encoding == Encoding.UTF32))
            {
                encoding = Encoding.UTF32;
            }
            else if (result.Details.Any(detail => detail.Encoding == Encoding.Default))
            {
                encoding = Encoding.Default;
            }
        }

        // When the text is too short and lacks a BOM
        // In this case, we should fallback to an encoding if it is not recognized as UTF8 / UTF32 / ANSI
        // https://github.com/QL-Win/QuickLook/issues/471
        // https://github.com/QL-Win/QuickLook/issues/600
        // https://github.com/QL-Win/QuickLook/issues/954
        if (buffer.Length <= 50)
        {
            if (encoding != Encoding.UTF8 && encoding != Encoding.UTF32 && encoding != Encoding.Default)
            {
                if (!Encoding.UTF8.GetString(buffer).Contains("\uFFFD"))
                {
                    encoding = Encoding.UTF8;
                }
                else if (!Encoding.UTF32.GetString(buffer).Contains("\uFFFD"))
                {
                    encoding = Encoding.UTF32;
                }
                else if (!Encoding.Default.GetString(buffer).Contains("\uFFFD"))
                {
                    encoding = Encoding.Default;
                }
            }
        }

        return encoding;
    }
}
