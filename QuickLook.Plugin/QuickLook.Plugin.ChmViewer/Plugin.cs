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
    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return false;
#pragma warning disable CS0162 // Unreachable code detected
        return !Directory.Exists(path)
            && Path.GetExtension(path).Equals(".chm", StringComparison.OrdinalIgnoreCase);
#pragma warning restore CS0162 // Unreachable code detected
    }

    public void Prepare(string path, ContextObject context)
    {
        context.Title = Path.GetFileName(path);
        context.IsBlocked = true;
        context.PreferredSize = new Size { Width = 800, Height = 600 };
    }

    public void View(string path, ContextObject context)
    {
        context.IsBusy = false;
    }

    public void Cleanup()
    {
    }
}
