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

using QuickLook.Common.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuickLook.Plugin.TextViewer;

public class Plugin : IViewer
{
    private static readonly HashSet<string> WellKnownExtensions = new(
    [
        ".txt", ".rtf",
    ]);

    private TextViewerPanel _tvp;

    public int Priority => -5;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        if (Directory.Exists(path))
            return false;

        if (WellKnownExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Read the first 16KB, check if we can get something
        using var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        const int bufferLength = 16 * 1024;
        var buffer = new byte[bufferLength];
        var size = s.Read(buffer, 0, bufferLength);

        return IsText(buffer, size);
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 800, Height = 600 };
    }

    public void View(string path, ContextObject context)
    {
        if (path.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase))
        {
            var rtfBox = new RichTextBox();
            using FileStream fs = File.OpenRead(path);
            rtfBox.Background = new SolidColorBrush(Colors.Transparent);
            rtfBox.Selection.Load(fs, DataFormats.Rtf);
            rtfBox.IsReadOnly = true;
            rtfBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            rtfBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            context.ViewerContent = rtfBox;
            context.IsBusy = false;
        }
        else
        {
            _tvp = new TextViewerPanel();
            _tvp.LoadFileAsync(path, context);
            context.ViewerContent = _tvp;
        }
        context.Title = Path.GetFileName(path);
    }

    public void Cleanup()
    {
        _tvp?.Dispose();
        _tvp = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsText(IReadOnlyList<byte> buffer, int size)
    {
        for (var i = 1; i < size; i++)
            if (buffer[i - 1] == 0 && buffer[i] == 0)
                return false;

        return true;
    }
}
