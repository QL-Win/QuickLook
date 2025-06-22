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

using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using QuickLook.Common.Helpers;
using QuickLook.Plugin.TextViewer.Detectors;
using QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using DarkHighlightingDefinition = QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Dark;
using LightHighlightingDefinition = QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Light;

namespace QuickLook.Plugin.TextViewer.Themes;

public class HighlightingThemeManager
{
    public static HighlightingManager Light { get; internal set; }

    public static HighlightingManager Dark { get; internal set; }

    public static void Initialize()
    {
        InitHighlightingManager();
        InitCustomHighlighting();
    }

    public static HighlightingTheme GetHighlightingByExtensionOrDetector(string extension, string text = null)
    {
        if (Light is null || Dark is null) return HighlightingTheme.Default;

        var highlightingTheme = GetDefinitionByExtension(nameof(Dark), extension);

        if (highlightingTheme == null)
        {
            highlightingTheme = GetDefinitionByExtension(nameof(Light), extension);

            if (highlightingTheme == null)
            {
                var useFormatDetector = SettingHelper.Get("UseFormatDetector", true, "QuickLook.Plugin.TextViewer");

                if (useFormatDetector && FormatDetector.Detect(text)?.Extension is string detectExtension)
                {
                    highlightingTheme = GetDefinitionByExtension(nameof(Dark), detectExtension)
                                     ?? GetDefinitionByExtension(nameof(Light), detectExtension);
                }
            }
        }

        // Unsupported highlighting
        highlightingTheme ??= HighlightingTheme.Default;

        var darkThemeAllowed = SettingHelper.Get("AllowDarkTheme", highlightingTheme.IsDark, "QuickLook.Plugin.TextViewer");
        var isDark = darkThemeAllowed && OSThemeHelper.AppsUseDarkTheme();

        // The current environment does not require dark mode so revert to light mode
        if (!isDark && highlightingTheme.IsDark)
        {
            highlightingTheme.Theme = nameof(Light);
            highlightingTheme.HighlightingManager = Light;
            highlightingTheme.SyntaxHighlighting
                // The extension that supports dark mode must support light mode also
                = Light.GetDefinitionByExtension(highlightingTheme.Extension);
        }

        return highlightingTheme;
    }

    private static HighlightingTheme GetDefinitionByExtension(string theme, string extension)
    {
        var highlightingManager = theme == nameof(Dark) ? Dark : Light;
        var def = highlightingManager.GetDefinitionByExtension(extension);

        if (def != null)
        {
            return new HighlightingTheme()
            {
                Theme = theme,
                HighlightingManager = highlightingManager,
                SyntaxHighlighting = def,
                Extension = extension,
            };
        }
        return null;
    }

    private static void InitHighlightingManager()
    {
        Light = new HighlightingManager();
        Dark = new HighlightingManager();

        Assembly assembly = Assembly.GetExecutingAssembly();
        string[] resourceNames = assembly.GetManifestResourceNames();

        foreach (var resourceName in resourceNames.Where(name => name.Contains(".Syntax.")))
        {
            using Stream s = assembly.GetManifestResourceStream(resourceName);

            if (s == null)
                continue;

            Debug.WriteLine(resourceName);

            try
            {
                var hlm = resourceName.Contains(".Syntax.Dark.") ? Dark : Light;
                var name = EmbeddedResource.GetFileNameWithoutExtension(resourceName);
                using var reader = new XmlTextReader(s);
                var xshd = HighlightingLoader.LoadXshd(reader);
                var highlightingDefinition = HighlightingLoader.Load(xshd, hlm);
                if (xshd.Extensions.Count > 0)
                    hlm.RegisterHighlighting(name, [.. xshd.Extensions], highlightingDefinition);
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
            }
        }

        AddHighlightingManager(Light, nameof(Light));
        AddHighlightingManager(Dark, nameof(Dark));
    }

    private static void AddHighlightingManager(HighlightingManager hlm, string dirName)
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(assemblyPath))
            return;

        var syntaxPath = Path.Combine(assemblyPath, "Syntax", dirName);
        if (!Directory.Exists(syntaxPath))
            return;

        foreach (var file in Directory.EnumerateFiles(syntaxPath, "*.xshd").OrderBy(f => f))
        {
            try
            {
                Debug.WriteLine(file);
                var ext = Path.GetFileNameWithoutExtension(file);
                using Stream s = File.OpenRead(Path.GetFullPath(file));
                using var reader = new XmlTextReader(s);
                var xshd = HighlightingLoader.LoadXshd(reader);
                var highlightingDefinition = HighlightingLoader.Load(xshd, hlm);
                if (xshd.Extensions.Count > 0)
                    hlm.RegisterHighlighting(ext, [.. xshd.Extensions], highlightingDefinition);
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
            }
        }
    }

    private static void InitCustomHighlighting()
    {
        AddCustomHighlighting(Light, new LightHighlightingDefinition.PropertiesHighlightingDefinition());

        AddCustomHighlighting(Dark, new DarkHighlightingDefinition.PropertiesHighlightingDefinition());
    }

    private static void AddCustomHighlighting(HighlightingManager hlm, CustomHighlightingDefinition definition)
    {
        try
        {
            hlm.RegisterHighlighting(definition.Name, definition.Extension.Split(';'), definition);
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
        }
    }
}

file static class EmbeddedResource
{
    public static string GetFileNameWithoutExtension(string resourceName)
    {
        // Requires the embedded resource file name
        // must have a file extension and have only one '.' character
        int start = int.MinValue, end = int.MinValue;

        for (int i = resourceName.Length - 1; i >= 0; i--)
        {
            if (resourceName[i] == '.')
            {
                if (end == int.MinValue)
                {
                    end = i;
                    continue;
                }

                if (start == int.MinValue)
                {
                    start = i + 1; // Exinclude '.' character
                    break;
                }
            }
        }

        if ((start != int.MinValue) && (end != int.MinValue))
        {
            return resourceName.Substring(start, end - start);
        }
        return resourceName;
    }
}
