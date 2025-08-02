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
using System;
using System.Diagnostics;
using System.IO;

namespace QuickLook.Plugin.ThumbnailViewer.Providors;

internal class XmindProvidor : AbstractProvidor
{
    public override Stream ViewImage(string path)
    {
        try
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
        catch (Exception ex)
        {
            Debug.WriteLine($"Error reading thumbnail from {path}: {ex.Message}");
            ProcessHelper.WriteLog(ex.ToString());
        }

        return null;
    }
}
