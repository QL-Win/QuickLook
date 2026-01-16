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

using QuickLook.Common.Plugin;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ThumbnailViewer.Providors;

internal abstract class AbstractProvidor
{
    public virtual void Prepare(string path, ContextObject context)
    {
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

    public abstract Stream ViewImage(string path);
}
