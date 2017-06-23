﻿// Copyright © 2017 Paddy Xu
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

using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using QuickLook.Plugin.HtmlViewer;

namespace QuickLook.Plugin.MarkdownViewer
{
    public class Plugin : IViewer
    {
        private WebpagePanel _panel;

        public int Priority => int.MaxValue;
        public bool AllowsTransparency => false;

        public void Init()
        {
        }

        public bool CanHandle(string path)
        {
            if (Directory.Exists(path))
                return false;

            switch (Path.GetExtension(path).ToLower())
            {
                case ".markdown":
                case ".md":
                case ".rmd":
                    return true;

                default:
                    return false;
            }
        }

        public void Prepare(string path, ContextObject context)
        {
            context.PreferredSize = new Size(1000, 600);

            context.CanFocus = true;
        }

        public void View(string path, ContextObject context)
        {
            _panel = new WebpagePanel();
            context.ViewerContent = _panel;
            context.Title = Path.GetFileName(path);

            _panel.LoadHtml(GenerateMarkdownHtml(path));
            _panel.Dispatcher.Invoke(() => { context.IsBusy = false; }, DispatcherPriority.Loaded);
        }

        public void Cleanup()
        {
            GC.SuppressFinalize(this);

            _panel?.Dispose();
            _panel = null;
        }

        ~Plugin()
        {
            Cleanup();
        }

        private string GenerateMarkdownHtml(string path)
        {
            var md = File.ReadAllText(path);
            md = md.Replace("&", "&amp;")
                   .Replace("<", "&lt;")
                   .Replace(">", "&gt;");
            var html = Resources.md2html.Replace("{{content}}", md);

            return html;
        }
    }
}