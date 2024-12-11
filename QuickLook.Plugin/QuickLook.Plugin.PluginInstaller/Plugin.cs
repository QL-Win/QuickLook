// Copyright © 2018 Paddy Xu
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

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Plugin;
using System;
using System.IO;
using System.Windows;

namespace QuickLook.Plugin.PluginInstaller;

public class Plugin : IViewer
{
    public int Priority => int.MaxValue;

    public void Init()
    {
        CleanupOldPlugins(App.UserPluginPath);
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path) && path.ToLower().EndsWith(".qlplugin");
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 460, Height = 200 };

        context.Title = string.Empty;
        context.TitlebarOverlap = false;
        context.TitlebarBlurVisibility = false;
        context.TitlebarColourVisibility = false;
        context.CanResize = false;
        context.FullWindowDragging = true;
    }

    public void View(string path, ContextObject context)
    {
        context.ViewerContent = new PluginInfoPanel(path, context);

        context.IsBusy = false;
    }

    public void Cleanup()
    {
    }

    private static void CleanupOldPlugins(string folder)
    {
        if (!Directory.Exists(folder))
            return;

        Directory.GetFiles(folder, "*.to_be_deleted", SearchOption.AllDirectories).ForEach(file =>
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception)
            {
                // ignored
            }
        });
    }
}
