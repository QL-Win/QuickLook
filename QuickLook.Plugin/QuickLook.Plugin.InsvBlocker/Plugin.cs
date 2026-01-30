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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QuickLook.Plugin.InsvBlocker;

public class Plugin : IViewer
{
    // Very high priority to ensure this plugin is checked before any other plugins
    // This prevents QuickLook from handling .insv files, allowing Insta360Studio's QuickLook to handle them instead
    public int Priority => int.MaxValue;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        // Match .insv files (Insta360 panoramic video files)
        if (Directory.Exists(path))
            return false;

        return path.EndsWith(".insv", StringComparison.OrdinalIgnoreCase);
    }

    public void Prepare(string path, ContextObject context)
    {
        // Set Ignore to true to display "blocked" in the preview window
        context.IsBlocked = true;
        context.Title = $"[BLOCKED] {Path.GetFileName(path)}";
        context.PreferredSize = new Size(400, 200);
    }

    public void View(string path, ContextObject context)
    {
        // This should not be called since Ignore is set to true in Prepare
        // But if called, do nothing
    }

    public void Cleanup()
    {
    }
}
