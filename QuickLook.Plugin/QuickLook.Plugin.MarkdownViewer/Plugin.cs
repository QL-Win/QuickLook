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

using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.HtmlViewer;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using UtfUnknown;

namespace QuickLook.Plugin.MarkdownViewer;

public class Plugin : IViewer
{
    private bool _isInitialized = false;
    private WebpagePanel? _panel;
    private string? _currentHtmlPath;

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

    private static readonly string _resourcePath = Path.Combine(SettingHelper.LocalDataPath, "QuickLook.Plugin.MarkdownViewer");
    private static readonly string _resourcePrefix = "QuickLook.Plugin.MarkdownViewer.Resources.";
    private static readonly ResourceManager _resourceManager = new(_resourcePath, _resourcePrefix);

    public int Priority => 0;

    public void Init()
    {
        // Delayed initialization can speed up startup
        _isInitialized = false;
    }

    public void InitializeResources()
    {
        // Initialize resources and handle versioning
        _resourceManager.InitializeResources();

        // Clean up any temporary HTML files if QuickLook was forcibly terminated
        CleanupTempFiles();
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
        if (!_isInitialized)
        {
            _isInitialized = true;
            InitializeResources();
        }

        _panel = new WebpagePanel();
        context.ViewerContent = _panel;
        context.Title = Path.GetFileName(path);

        var htmlPath = GenerateMarkdownHtml(path);
        _panel.FallbackPath = Path.GetDirectoryName(path);
        _panel.NavigateToFile(htmlPath);
        _panel.Dispatcher.Invoke(() => { context.IsBusy = false; }, DispatcherPriority.Loaded);
    }

    private string GenerateMarkdownHtml(string path)
    {
        var templatePath = Path.Combine(_resourcePath, "md2html.html");

        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Required template file md2html.html not found in extracted resources at {templatePath}");

        var bytes = File.ReadAllBytes(path);
        var encoding = CharsetDetector.DetectFromBytes(bytes).Detected?.Encoding ?? Encoding.Default;
        var content = encoding.GetString(bytes);

        var template = File.ReadAllText(templatePath);
        var html = template.Replace("{{content}}", content);

        // Generate unique filename and ensure it doesn't exist
        string outputPath;
        do
        {
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var outputFileName = $"temp_{uniqueId}.html";
            outputPath = Path.Combine(_resourcePath, outputFileName);
        } while (File.Exists(outputPath));

        // Clean up previous file if it exists
        CleanupTempHtmlFile();

        File.WriteAllText(outputPath, html);
        _currentHtmlPath = outputPath;

        return outputPath;
    }

    #region Cleanup

    private void CleanupTempHtmlFile()
    {
        if (!string.IsNullOrEmpty(_currentHtmlPath) && File.Exists(_currentHtmlPath))
        {
            try
            {
                File.Delete(_currentHtmlPath);
            }
            catch (IOException) { } // Ignore deletion errors
        }
    }

    private void CleanupTempFiles()
    {
        try
        {
            var tempFiles = Directory.GetFiles(_resourcePath, "temp_*.html");
            foreach (var file in tempFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException) { } // Ignore deletion errors
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to clean up temporary HTML files: {ex.Message}");
        }
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        CleanupTempHtmlFile();

        _panel?.Dispose();
        _panel = null;
    }

    #endregion Cleanup
}
