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
using QuickLook.Common.Plugin.MoreMenu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace QuickLook.Plugin.BinaryViewer;

public sealed partial class Plugin : IViewer, IMoreMenuExtended
{
    private static readonly HashSet<string> SupportedExtensions =
    [
        ".bin", ".hex",
    ];

    private BinaryViewerPanel _panel;

    public int Priority => -10;

    public IEnumerable<IMenuItem> MenuItems => GetMenuItems();

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        if (Directory.Exists(path))
            return false;

        return SupportedExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 900, Height = 600 };
    }

    public void View(string path, ContextObject context)
    {
        _panel = new BinaryViewerPanel();
        context.ViewerContent = _panel;

        _panel.LoadFile(path);

        context.Title = Path.GetFileName(path);
        context.IsBusy = false;
    }

    public void Cleanup()
    {
        _panel?.Unload();
        _panel = null;
    }
}
