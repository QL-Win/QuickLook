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
using System.IO;
using System.Linq;
using System.Windows;

namespace QuickLook.Plugin.DumpViewer;

public sealed class Plugin : IViewer
{
    private static readonly string[] _extensions =
    [
        ".dmp", ".dump", ".mdmp", ".hdmp", ".minidump",
    ];

    private DumpInfoPanel _panel;

    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        if (Directory.Exists(path))
            return false;

        var pathLower = path.ToLowerInvariant();
        if (!_extensions.Any(pathLower.EndsWith) && !MinidumpReader.IsMinidump(path))
            return false;

        return MinidumpReader.IsMinidump(path);
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 1024, Height = 720 };
        context.Title = Path.GetFileName(path);
        context.TitlebarOverlap = false;
        context.TitlebarBlurVisibility = false;
        context.TitlebarColourVisibility = false;
    }

    public void View(string path, ContextObject context)
    {
        _panel = new DumpInfoPanel();
        _panel.DisplayInfo(path);

        context.ViewerContent = _panel;
        context.Title = Path.GetFileName(path);
        context.IsBusy = false;
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _panel = null;
    }
}
