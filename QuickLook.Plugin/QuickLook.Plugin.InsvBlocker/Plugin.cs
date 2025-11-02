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
        // Set a minimal window size
        context.PreferredSize = new Size { Width = 1, Height = 1 };
    }

    public void View(string path, ContextObject context)
    {
        // Create an empty text block as content (required to avoid errors)
        var textBlock = new TextBlock
        {
            Text = "",
            Visibility = Visibility.Collapsed
        };
        context.ViewerContent = textBlock;
        context.IsBusy = false;
        
        // Close the window immediately using a dispatcher
        Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
        {
            if (context.Source is Window window)
            {
                window.Close();
            }
        }), DispatcherPriority.ApplicationIdle);
    }

    public void Cleanup()
    {
    }
}
