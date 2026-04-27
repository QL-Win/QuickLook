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
using System.Windows;

namespace QuickLook.Plugin.ChmViewer;

public sealed class Plugin : IViewer
{
    private ChmWebpagePanel _panel;

    /// <summary>
    /// The implementation of this plugin is better than following
    /// https://github.com/emako/QuickLook.Plugin.SumatraPDFReader
    /// </summary>
    public int Priority => 2;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path)
            && Path.GetExtension(path).Equals(".chm", StringComparison.OrdinalIgnoreCase);
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size(1280, 720);
    }

    public void View(string path, ContextObject context)
    {
        _panel = new ChmWebpagePanel();
        _panel.PreviewCompiledHtmlHelp(path);

        context.ViewerContent = _panel;
        context.Title = Path.GetFileName(path);
        context.IsBusy = false;
    }

    public void Cleanup()
    {
        _panel?.Dispose();
        _panel = null;
        GC.SuppressFinalize(this);
    }
}
