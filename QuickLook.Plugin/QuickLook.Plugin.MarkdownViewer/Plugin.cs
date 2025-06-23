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

namespace QuickLook.Plugin.MarkdownViewer;

public class Plugin : IViewer
{
    private MarkdownPanel _panel;

    /// <summary>
    /// Markdown and Markdown-like extensions
    /// It is not guaranteed to support all formats perfectly
    /// </summary>
    private static readonly string[] _extensions =
    [
        ".md", ".markdown", // The most common Markdown extensions
        ".mdx", // MDX (Markdown + JSX), used in React ecosystems
        ".mmd", // MultiMarkdown (MMD), an extended version of Markdown
        ".mkd", ".mdwn", ".mdown", // Early Markdown variants, used by different parsers like Pandoc, Gitit, and Hakyll
        ".mdc", // A Markdown variant used by Cursor AI [Repeated format from ImageViewer]
        ".qmd", // Quarto Markdown, developed by RStudio for scientific computing and reproducible reports
        ".rmd", ".rmarkdown", // R Markdown, mainly used in RStudio
        ".apib", // API Blueprint, a Markdown-based format
        ".mdtxt", ".mdtext", // Less common
    ];

    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path) && _extensions.Any(path.ToLower().EndsWith);
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size(1000, 600);
    }

    public void View(string path, ContextObject context)
    {
        _panel = new MarkdownPanel();
        _panel.PreviewMarkdown(path);

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
