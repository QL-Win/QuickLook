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
using System.Windows.Threading;

namespace QuickLook.Plugin.HtmlViewer;

public sealed partial class Plugin : IViewer, IMoreMenu
{
    private static readonly string[] _extensions = [".mht", ".mhtml", ".htm", ".html"];
    private static readonly string[] _supportedProtocols = ["http", "https"];

    private WebpagePanel _panel;
    private string _currentPath;

    public int Priority => 0;

    public IEnumerable<IMenuItem> MenuItems => GetMenuItems();

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path) && (_extensions.Any(path.ToLower().EndsWith) ||
                                           path.ToLower().EndsWith(".url") &&
                                           _supportedProtocols.Contains(Helper.GetUrlPath(path).Split(':')[0]
                                               .ToLower()));
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size(1280, 720);
    }

    public void View(string path, ContextObject context)
    {
        _panel = new WebpagePanel();
        _currentPath = path;
        context.ViewerContent = _panel;
        context.Title = Path.IsPathRooted(path) ? Path.GetFileName(path) : path;

        if (path.ToLower().EndsWith(".url"))
            path = Helper.GetUrlPath(path);
        _panel.FallbackPath = Path.GetDirectoryName(path); // Reserve opportunities for requesting exceeds MAX_PATH (260) limitation
        _panel.NavigateToFile(path);
        _panel.Dispatcher.Invoke(() => { context.IsBusy = false; }, DispatcherPriority.Loaded);
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _panel?.Dispose();
        _panel = null;
    }
}
