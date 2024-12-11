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

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;

namespace QuickLook.Plugin.TextViewer;

public class Plugin : IViewer
{
    private TextViewerPanel _tvp;

    private static HighlightingManager _hlmLight;
    private static HighlightingManager _hlmDark;

    public int Priority => -5;

    public void Init()
    {
        // pre-load
        var _ = new TextEditor();

        _hlmLight = getHighlightingManager(Themes.Light, "Light");
        _hlmDark = getHighlightingManager(Themes.Dark, "Dark");
    }

    public bool CanHandle(string path)
    {
        if (Directory.Exists(path))
            return false;

        if (new[] { ".txt", ".rtf" }.Any(path.ToLower().EndsWith))
            return true;

        // if there is a matched highlighting scheme (by file extension), treat it as a plain text file
        //if (HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(path)) != null)
        //    return true;

        // otherwise, read the first 16KB, check if we can get something.
        using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            const int bufferLength = 16 * 1024;
            var buffer = new byte[bufferLength];
            var size = s.Read(buffer, 0, bufferLength);

            return IsText(buffer, size);
        }
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 800, Height = 600 };
    }

    public void View(string path, ContextObject context)
    {
        if (path.ToLower().EndsWith(".rtf"))
        {
            var rtfBox = new RichTextBox();
            FileStream fs = File.OpenRead(path);
            rtfBox.Selection.Load(fs, DataFormats.Rtf);
            rtfBox.IsReadOnly = true;
            rtfBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            rtfBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            context.ViewerContent = rtfBox;
            context.IsBusy = false;
        }
        else
        {
            _tvp = new TextViewerPanel(path, context);
            AssignHighlightingManager(_tvp, context);

            context.ViewerContent = _tvp;
        }
        context.Title = $"{Path.GetFileName(path)}";
    }

    public void Cleanup()
    {
        _tvp?.Dispose();
        _tvp = null;
    }

    private static bool IsText(IReadOnlyList<byte> buffer, int size)
    {
        for (var i = 1; i < size; i++)
            if (buffer[i - 1] == 0 && buffer[i] == 0)
                return false;

        return true;
    }

    private HighlightingManager getHighlightingManager(Themes theme, string dirName)
    {
        var hlm = new HighlightingManager();

        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(assemblyPath))
            return hlm;

        var syntaxPath = Path.Combine(assemblyPath, "Syntax", dirName);
        if (!Directory.Exists(syntaxPath))
            return hlm;

        foreach (var file in Directory.EnumerateFiles(syntaxPath, "*.xshd").OrderBy(f => f))
        {
            Debug.WriteLine(file);
            var ext = Path.GetFileNameWithoutExtension(file);
            using (Stream s = File.OpenRead(Path.GetFullPath(file)))
            using (var reader = new XmlTextReader(s))
            {
                var xshd = HighlightingLoader.LoadXshd(reader);
                var highlightingDefinition = HighlightingLoader.Load(xshd, hlm);
                if (xshd.Extensions.Count > 0)
                    hlm.RegisterHighlighting(ext, xshd.Extensions.ToArray(), highlightingDefinition);
            }
        }

        return hlm;
    }

    private void AssignHighlightingManager(TextViewerPanel tvp, ContextObject context)
    {
        var darkThemeAllowed = SettingHelper.Get("AllowDarkTheme", false, "QuickLook.Plugin.TextViewer");
        var isDark = darkThemeAllowed && OSThemeHelper.AppsUseDarkTheme();
        tvp.HighlightingManager = isDark ? _hlmDark : _hlmLight;
        if (isDark)
        {
            tvp.Background = Brushes.Transparent;
            tvp.SetResourceReference(Control.ForegroundProperty, "WindowTextForeground");
        }
        else
        {
            // if os dark mode, but not AllowDarkTheme, make background light
            tvp.Background = OSThemeHelper.AppsUseDarkTheme()
                ? new SolidColorBrush(Color.FromArgb(150, 255, 255, 255))
                : Brushes.Transparent;
        }
    }
}
