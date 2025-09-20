// Copyright © 2017-2025 QL-Win Contributors
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

using QuickLook.Common.ExtensionMethods;
using QuickLook.Plugin.VideoViewer.Extensions;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.VideoViewer.AudioTrack;

internal static class CoverDataExtractor
{
    /// <summary>
    /// Extracts cover image data from a Base64 string.
    /// </summary>
    /// <param name="base64strings">A Base64-encoded string (may contain multiple covers separated by " / ").</param>
    /// <returns>Byte array of the cover image, or null if extraction fails.</returns>
    public static byte[] Extract(string base64strings)
    {
        try
        {
            var coverData = base64strings.Trim();

            if (!string.IsNullOrEmpty(coverData))
            {
                // MediaInfo may return multiple covers in one string.
                // In that case, only take the first one.
                var coverBytes = Convert.FromBase64String
                (
                    coverData.Contains(' ')
                        ? coverData.Split(" / ")[0]
                        : coverData
                );

                return coverBytes;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        return null;
    }

    /// <summary>
    /// Extracts a <see cref="BitmapSource"/> from cover image bytes.
    /// </summary>
    /// <param name="coverBytes">Cover image as a byte array.</param>
    /// <returns><see cref="BitmapSource"/> if successful; otherwise, null.</returns>
    public static BitmapSource Extract(byte[] coverBytes)
    {
        // Supported formats: JPEG, PNG, BMP, GIF (first frame only), TIFF (first page only), ICO.
        // Not supported: WebP, HEIC/HEIF, AVIF, JPEG 2000, animated WebP/APNG, and other raw image formats (e.g., CR2, NEF).

        using var ms = new MemoryStream(coverBytes);

        try
        {
            // First try decoding with WPF's built-in decoder.
            var coverArt = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            return coverArt;
        }
        catch
        {
            // Fallback:
            // https://github.com/QL-Win/QuickLook/issues/1759
            // Use System.Drawing's Bitmap decoder, which is more forgiving with common JPEG files
            // and tends not to throw WINCODEC_ERR_STREAMREAD like WPF often does.
            try
            {
                using var bmp = new Bitmap(ms);
                var coverArt = bmp.ToBitmapSource();
                return coverArt;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        return null;
    }
}
