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

using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Nuget;

public static class NugetParser
{
    public static NugetInfo Parse(string path)
    {
        NugetInfo info = new();

        try
        {
            using var zip = new ZipFile(path);

            // Find .nuspec entry (only one should exist at the root or in a subdirectory)
            ZipEntry nuspecEntry = null;
            foreach (ZipEntry entry in zip)
            {
                if (!entry.IsDirectory && entry.Name.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
                {
                    nuspecEntry = entry;
                    break;
                }
            }

            if (nuspecEntry == null) return info;

            // Parse nuspec XML
            XDocument doc;
            using (var stream = zip.GetInputStream(nuspecEntry))
            {
                doc = XDocument.Load(stream);
            }

            // Handle XML namespace (nuspec uses versioned namespace)
            XNamespace ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            var metadata = doc.Root?.Element(ns + "metadata");
            if (metadata == null) return info;

            info.PackageId = metadata.Element(ns + "id")?.Value?.Trim();
            info.Version = metadata.Element(ns + "version")?.Value?.Trim();
            info.Authors = metadata.Element(ns + "authors")?.Value?.Trim();
            info.Description = metadata.Element(ns + "description")?.Value?.Trim();
            info.ProjectUrl = metadata.Element(ns + "projectUrl")?.Value?.Trim();

            // Repository URL
            var repoEl = metadata.Element(ns + "repository");
            if (repoEl != null)
            {
                info.RepositoryUrl = repoEl.Attribute("url")?.Value?.Trim();
            }

            // License: prefer <license> element, fall back to <licenseUrl>
            var licenseEl = metadata.Element(ns + "license");
            if (licenseEl != null)
            {
                info.License = licenseEl.Value?.Trim();
            }
            else
            {
                info.License = metadata.Element(ns + "licenseUrl")?.Value?.Trim();
            }

            // Icon — look up the embedded icon file in the zip
            var iconPath = metadata.Element(ns + "icon")?.Value?.Trim();
            if (!string.IsNullOrEmpty(iconPath))
            {
                // Normalize path separators for comparison
                string normalizedIconPath = iconPath.Replace('\\', '/');
                foreach (ZipEntry entry in zip)
                {
                    if (!entry.IsDirectory &&
                        string.Equals(entry.Name.Replace('\\', '/'), normalizedIconPath, StringComparison.OrdinalIgnoreCase))
                    {
                        using var iconStream = zip.GetInputStream(entry);
                        using var ms = new MemoryStream();
                        iconStream.CopyTo(ms);
                        ms.Position = 0;
                        // Clone the bitmap so it doesn't depend on the stream
                        using var tmpBitmap = new Bitmap(ms);
                        info.Icon = new Bitmap(tmpBitmap);
                        break;
                    }
                }
            }

            // Target Frameworks and Dependencies
            var depsEl = metadata.Element(ns + "dependencies");
            if (depsEl != null)
            {
                var groups = depsEl.Elements(ns + "group").ToList();

                if (groups.Count > 0)
                {
                    // Collect distinct target frameworks
                    info.TargetFrameworks = [.. groups
                        .Select(g => g.Attribute("targetFramework")?.Value?.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Distinct()];

                    // Format all dependencies grouped by framework
                    var depLines = new List<string>();
                    foreach (var group in groups)
                    {
                        string fw = group.Attribute("targetFramework")?.Value?.Trim();
                        var deps = group.Elements(ns + "dependency").ToList();
                        if (deps.Count == 0) continue;

                        if (!string.IsNullOrEmpty(fw))
                            depLines.Add($"[{fw}]");

                        foreach (var dep in deps)
                        {
                            string id = dep.Attribute("id")?.Value?.Trim() ?? string.Empty;
                            string ver = dep.Attribute("version")?.Value?.Trim() ?? string.Empty;
                            string exclude = dep.Attribute("exclude")?.Value?.Trim();
                            string line = string.IsNullOrEmpty(ver) ? id : $"{id} ({ver})";
                            depLines.Add($"  {line}");
                        }
                    }
                    info.Dependencies = [.. depLines];
                }
                else
                {
                    // No groups — direct top-level dependencies
                    info.Dependencies = [.. depsEl.Elements(ns + "dependency")
                        .Select(d =>
                        {
                            string id = d.Attribute("id")?.Value?.Trim() ?? string.Empty;
                            string ver = d.Attribute("version")?.Value?.Trim() ?? string.Empty;
                            return string.IsNullOrEmpty(ver) ? id : $"{id} ({ver})";
                        })
                        .Where(s => !string.IsNullOrWhiteSpace(s))];
                }
            }
        }
        catch
        {
            // Return whatever partial info was collected
        }

        return info;
    }
}
