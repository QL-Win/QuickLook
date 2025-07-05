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
using QuickLook.Common.Plugin;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace QuickLook.Plugin.ThumbnailViewer;

internal static class Handler
{
    public static void Prepare(string path, ContextObject context)
    {
        if (path.EndsWith(".xd", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using Stream imageData = ViewImage(path);
                BitmapImage bitmap = imageData.ReadAsBitmapImage();
                context.SetPreferredSizeFit(new Size(bitmap.PixelWidth, bitmap.PixelHeight), 0.8d);
            }
            catch (Exception e)
            {
                _ = e;
                context.PreferredSize = new Size { Width = 1200, Height = 900 };
            }
        }
        else if (path.EndsWith(".fig", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using Stream imageData = ViewImage(path);
                BitmapImage bitmap = imageData.ReadAsBitmapImage();
                context.PreferredSize = new Size { Width = bitmap.PixelWidth * 1.4d, Height = bitmap.PixelHeight * 1.8d };
            }
            catch (Exception e)
            {
                _ = e;
                context.PreferredSize = new Size { Width = 100, Height = 100 };
            }
        }
        else if (path.EndsWith(".xmind", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using Stream imageData = ViewImage(path);
                BitmapImage bitmap = imageData.ReadAsBitmapImage();
                context.SetPreferredSizeFit(new Size(bitmap.PixelWidth, bitmap.PixelHeight), 0.8d);
            }
            catch (Exception e)
            {
                _ = e;
                context.PreferredSize = new Size { Width = 1200, Height = 900 };
            }
        }
        else if (path.EndsWith(".kra", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using Stream imageData = ViewImage(path);
                BitmapImage bitmap = imageData.ReadAsBitmapImage();
                context.SetPreferredSizeFit(new Size(bitmap.PixelWidth, bitmap.PixelHeight), 0.8d);
            }
            catch (Exception e)
            {
                _ = e;
                context.PreferredSize = new Size { Width = 800, Height = 600 };
            }
        }
        else if (path.EndsWith(".cdr", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using Stream imageData = ViewImage(path);
                BitmapImage bitmap = imageData.ReadAsBitmapImage();
                context.SetPreferredSizeFit(new Size(bitmap.PixelWidth, bitmap.PixelHeight), 0.8d);
            }
            catch (Exception e)
            {
                _ = e;
                context.PreferredSize = new Size { Width = 800, Height = 600 };
            }
        }
    }

    public static Stream ViewImage(string path)
    {
        if (path.EndsWith(".xd", StringComparison.OrdinalIgnoreCase))
        {
            using ZipArchive archive = ZipArchive.Open(path, new());
            using IReader reader = archive.ExtractAllEntries();

            while (reader.MoveToNextEntry())
            {
                if (reader.Entry.Key!.Equals("preview.png", StringComparison.OrdinalIgnoreCase))
                {
                    MemoryStream ms = new();
                    using EntryStream stream = reader.OpenEntryStream();
                    stream.CopyTo(ms);
                    return ms;
                }
                if (reader.Entry.Key!.Equals("thumbnail.png", StringComparison.OrdinalIgnoreCase))
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
            try
            {
                using ZipArchive archive = ZipArchive.Open(path, new());
                using IReader reader = archive.ExtractAllEntries();

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
            catch
            {
                ///
            }

            StreamResourceInfo info = Application.GetResourceStream(new Uri("pack://application:,,,/QuickLook.Plugin.ThumbnailViewer;component/Resources/broken.png"));
            return info?.Stream;
        }
        else if (path.EndsWith(".xmind", StringComparison.OrdinalIgnoreCase))
        {
            using ZipArchive archive = ZipArchive.Open(path, new());
            using IReader reader = archive.ExtractAllEntries();

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
            using ZipArchive archive = ZipArchive.Open(path, new());
            using IReader reader = archive.ExtractAllEntries();

            while (reader.MoveToNextEntry())
            {
                Debug.WriteLine(reader.Entry.Key);

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
            using ZipArchive archive = ZipArchive.Open(path, new());
            using IReader reader = archive.ExtractAllEntries();

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

        return null;
    }
}
