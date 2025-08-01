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

using PureSharpCompress.Archives.Zip;
using PureSharpCompress.Common;
using PureSharpCompress.Readers;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ThumbnailViewer;

internal static class Handler
{
    // List<Pair<formats, type>>
    public static List<KeyValuePair<string[], Type>> Providers = [];

    public static void Prepare(string path, ContextObject context)
    {
        // Temporary codes
        if (path.EndsWith(".pdn", StringComparison.OrdinalIgnoreCase))
        {
            new PdnProvider().Prepare(path, context);
            return;
        }

        try
        {
            using Stream imageData = ViewImage(path);
            BitmapImage bitmap = imageData.ReadAsBitmapImage();
            context.SetPreferredSizeFit(new Size(bitmap.PixelWidth, bitmap.PixelHeight), 0.8d);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading thumbnail from {path}: {ex.Message}");
            context.PreferredSize = new Size { Width = 800, Height = 600 };
        }
    }

    public static Stream ViewImage(string path)
    {
        // Temporary codes
        if (path.EndsWith(".pdn", StringComparison.OrdinalIgnoreCase))
        {
            return new PdnProvider().ViewImage(path);
        }

        try
        {
            using ZipArchive archive = ZipArchive.Open(path, new());
            using IReader reader = archive.ExtractAllEntries();

            if (path.EndsWith(".xd", StringComparison.OrdinalIgnoreCase))
            {
                while (reader.MoveToNextEntry())
                {
                    if (reader.Entry.Key!.Equals("preview.png", StringComparison.OrdinalIgnoreCase))
                    {
                        MemoryStream ms = new();
                        using EntryStream stream = reader.OpenEntryStream();
                        stream.CopyTo(ms);
                        return ms;
                    }
                    else if (reader.Entry.Key!.Equals("thumbnail.png", StringComparison.OrdinalIgnoreCase))
                    {
                        MemoryStream ms = new();
                        using EntryStream stream = reader.OpenEntryStream();
                        stream.CopyTo(ms);
                        return ms;
                    }
                }
            }
            else if (path.EndsWith(".fig", StringComparison.OrdinalIgnoreCase))
            {
                while (reader.MoveToNextEntry())
                {
                    if (reader.Entry.Key!.Equals("thumbnail.png", StringComparison.OrdinalIgnoreCase))
                    {
                        MemoryStream ms = new();
                        using EntryStream stream = reader.OpenEntryStream();
                        stream.CopyTo(ms);
                        return ms;
                    }
                }
            }
            else if (path.EndsWith(".pip", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".pix", StringComparison.OrdinalIgnoreCase))
            {
                while (reader.MoveToNextEntry())
                {
                    if (reader.Entry.Key!.EndsWith(".thumb.png", StringComparison.OrdinalIgnoreCase))
                    {
                        MemoryStream ms = new();
                        using EntryStream stream = reader.OpenEntryStream();
                        stream.CopyTo(ms);
                        return ms;
                    }
                }
            }
            else if (path.EndsWith(".sketch", StringComparison.OrdinalIgnoreCase))
            {
                while (reader.MoveToNextEntry())
                {
                    if (reader.Entry.Key!.EndsWith("previews/preview.png", StringComparison.OrdinalIgnoreCase))
                    {
                        MemoryStream ms = new();
                        using EntryStream stream = reader.OpenEntryStream();
                        stream.CopyTo(ms);
                        return ms;
                    }
                }
            }
            else if (path.EndsWith(".xmind", StringComparison.OrdinalIgnoreCase))
            {
                while (reader.MoveToNextEntry())
                {
                    if (reader.Entry.Key!.Equals("Thumbnails/thumbnail.png", StringComparison.OrdinalIgnoreCase))
                    {
                        MemoryStream ms = new();
                        using EntryStream stream = reader.OpenEntryStream();
                        stream.CopyTo(ms);
                        return ms;
                    }
                }
            }
            else if (path.EndsWith(".kra", StringComparison.OrdinalIgnoreCase))
            {
                while (reader.MoveToNextEntry())
                {
                    if (reader.Entry.Key!.Contains("mergedimage"))
                    {
                        MemoryStream ms = new();
                        using EntryStream stream = reader.OpenEntryStream();
                        stream.CopyTo(ms);
                        return ms;
                    }
                }
            }
            else if (path.EndsWith(".cdr", StringComparison.OrdinalIgnoreCase))
            {
                while (reader.MoveToNextEntry())
                {
                    if (reader.Entry.Key!.Equals("previews/thumbnail.png", StringComparison.OrdinalIgnoreCase))
                    {
                        MemoryStream ms = new();
                        using EntryStream stream = reader.OpenEntryStream();
                        stream.CopyTo(ms);
                        return ms;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading thumbnail from {path}: {ex.Message}");
            ProcessHelper.WriteLog(ex.ToString());
        }

        return null;
    }
}
