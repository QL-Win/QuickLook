// Copyright © 2017 Paddy Xu
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

using MsgReader;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.HtmlViewer;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace QuickLook.Plugin.MailViewer;

public class Plugin : IViewer
{
    private WebpagePanel _panel;
    private string _tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path) && new[] { ".eml", ".msg" }.Any(path.ToLower().EndsWith);
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 1000, Height = 600 };
    }

    public void View(string path, ContextObject context)
    {
        _panel = new WebpagePanel();
        context.ViewerContent = _panel;
        context.Title = Path.GetFileName(path);

        _panel.NavigateToFile(ExtractMailBody(path));
        _panel.Dispatcher.Invoke(() => { context.IsBusy = false; }, DispatcherPriority.Loaded);
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _panel?.Dispose();
        _panel = null;

        if (Directory.Exists(_tmpDir))
            Directory.Delete(_tmpDir, true);
    }

    private string ExtractMailBody(string path)
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);

        var msg = new Reader();

        var files = msg.ExtractToFolder(path, _tmpDir, ReaderHyperLinks.Both);

        if (files.Length > 0 && !string.IsNullOrEmpty(files[0]))
            return files[0];

        throw new Exception($"{path} is not a valid msg file.");
    }
}
