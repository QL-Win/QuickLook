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
using System.Text.RegularExpressions;
using System.Windows;

namespace QuickLook.Plugin.CLSIDViewer;

public class Plugin : IViewer
{
    private CLSIDInfoPanel _ip;
    private string _path;

    public int Priority => -1;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Use Regex to check whether a string like "::{645FF040-5081-101B-9F08-00AA002F954E}"
        bool isCLSID = path.StartsWith("::")
            && Regex.IsMatch(path, @"^::\{[0-9A-Fa-f]{8}-([0-9A-Fa-f]{4}-){3}[0-9A-Fa-f]{12}\}$");

        return isCLSID;
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = path switch
        {
            CLSIDRegister.RecycleBin => new Size { Width = 400, Height = 150 },
            CLSIDRegister.ThisPC => new Size { Width = 900, Height = 800 },
            _ => new Size { Width = 520, Height = 192 },
        };
    }

    public void View(string path, ContextObject context)
    {
        _path = path;
        _ip = new CLSIDInfoPanel();
        _ip.DisplayInfo(path, context);

        context.ViewerContent = _ip;
        context.Title = $"{CLSIDRegister.GetName(path) ?? path}";
        context.IsBusy = true;
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _ip = null;
    }
}
