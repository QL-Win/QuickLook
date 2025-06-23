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
using System.IO;
using System.Linq;
using System.Windows;

namespace QuickLook.Plugin.FontViewer;

public class Plugin : IViewer
{
    private WebfontPanel _panel;

    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        // The `*.eot` and `*.svg` font types are not supported
        // TODO: Check `*.otc` type
        return !Directory.Exists(path) && new string[] { ".ttf", ".otf", ".woff", ".woff2", ".ttc" }.Any(path.ToLower().EndsWith);
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 1300, Height = 650 };
    }

    public void View(string path, ContextObject context)
    {
        _panel = new WebfontPanel();
        _panel.PreviewFont(path);

        context.ViewerContent = _panel;
        context.Title = Path.GetFileName(path);
        context.IsBusy = false;
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _panel?.Dispose();
        _panel = null;
    }
}
