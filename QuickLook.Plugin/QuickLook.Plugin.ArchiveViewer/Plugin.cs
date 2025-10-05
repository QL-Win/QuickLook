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

using System;
using System.IO;
using System.Linq;
using System.Windows;
using QuickLook.Common.Plugin;

namespace QuickLook.Plugin.ArchiveViewer;

public class Plugin : IViewer
{
    private static readonly string[] _extensions =
    [
        ".asar",    // Electron archive (used to package Electron app resources)
        ".7z",      // 7-Zip compressed archive (uses LZMA/LZMA2 compression)
        ".bz2",     // bzip2 compressed file (often used with tar, e.g. .tar.bz2)
        ".cb7",     // Comic book archive based on 7z format
        ".cbr",     // Comic book archive based on RAR format
        ".cbt",     // Comic book archive based on TAR format
        ".cbz",     // Comic book archive based on ZIP format
        ".crx",     // Chrome extension package (used for Chrome browser add-ons)
        ".gz",      // gzip compressed file (commonly used with tar, e.g. .tar.gz)
        ".jar",     // Java archive (used for Java applications; ZIP-based)
        ".lz",      // lzip compressed file (uses LZMA compression)
        ".nupkg",   // NuGet package (for distributing .NET libraries; ZIP-based)
        ".snupkg",  // Symbol NuGet package (stores debug symbols; ZIP-based)
        ".rar",     // RAR compressed archive (proprietary compression format)
        ".tar",     // TAR archive (packs multiple files without compression)
        ".tgz",     // Gzipped TAR archive (short for .tar.gz)
        ".vsix",    // Visual Studio extension package (ZIP-based)
        ".xz",      // XZ compressed file (uses LZMA2 compression)
        ".zip",     // ZIP compressed archive (most common compression format)
    ];

    private ArchiveInfoPanel _panel;

    public int Priority => -5;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path) && _extensions.Any(path.ToLower().EndsWith);
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
