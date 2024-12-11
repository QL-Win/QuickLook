// Copyright © 2017 Paddy Xu
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
using System.IO;
using System.Linq;
using System.Windows;
using QuickLook.Common.Plugin;

namespace QuickLook.Plugin.ArchiveViewer;

public class Plugin : IViewer
{
    private static readonly string[] Extensions =
        [".rar", ".zip", ".tar", ".tgz", ".gz", ".bz2", ".lz", ".xz", ".7z", ".jar", ".crx", ".nupkg", ".cb7", ".cbr", ".cbt", ".cbz"];

    private ArchiveInfoPanel _panel;

    public int Priority => -5;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path) && Extensions.Any(path.ToLower().EndsWith);
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 800, Height = 400 };
    }

    public void View(string path, ContextObject context)
    {
        _panel = new ArchiveInfoPanel(path);

        context.ViewerContent = _panel;
        context.Title = $"{Path.GetFileName(path)}";

        context.IsBusy = false;
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _panel?.Dispose();
        _panel = null;
    }
}
