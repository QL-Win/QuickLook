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
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Appx;

public class AppxBundleReader : IDisposable
{
    private ZipFile zip;
    private AppxReader appxReader = null;

    private string name;
    private string publisher;
    private string version;

    public string Name => name ?? appxReader?.Name;
    public string Publisher => publisher ?? appxReader?.Publisher;
    public string Version => version ?? appxReader?.Version;
    public string DisplayName => appxReader?.DisplayName;
    public string PublisherDisplayName => appxReader?.PublisherDisplayName;
    public string Description => appxReader?.Description;
    public string Logo => appxReader?.Logo;
    public string[] Capabilities => appxReader?.Capabilities;
    public Bitmap Icon => appxReader?.Icon;

    public AppxBundleReader(Stream stream)
    {
        Open(stream);
    }

    public AppxBundleReader(string path)
    {
        Open(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public void Dispose()
    {
        appxReader?.Dispose();
        appxReader = null;
        zip?.Close();
        zip = null;
    }

    private void Open(Stream stream)
    {
        zip = new ZipFile(stream);
        ZipEntry entry = zip.GetEntry("AppxMetadata/AppxBundleManifest.xml")
           ?? throw new InvalidDataException("AppxMetadata/AppxBundleManifest.xml not found");

        XmlDocument xml = new() { XmlResolver = null };
        xml.Load(zip.GetInputStream(entry));

        XmlElement bundleNode = xml.DocumentElement;

        // Identity
        {
            XmlElement identityNode = bundleNode["Identity"];

            if (identityNode != null)
            {
                name = identityNode.Attributes["Name"]?.Value;
                version = identityNode.Attributes["Version"]?.Value;
                publisher = identityNode.Attributes["Publisher"]?.Value;

                Match m = Regex.Match(Publisher, @"CN=([^,]*),?");
                if (m.Success) publisher = m.Groups[1].Value;
            }
        }

        // Packages
        {
            XmlElement packagesNode = bundleNode["Packages"];

            if (packagesNode != null)
            {
                string arch = RuntimeInformation.OSArchitecture == Architecture.Arm64
                ? "arm64"
                : Environment.Is64BitProcess ? "x64" : "x86";

                var packages = packagesNode.ChildNodes.Select(package =>
                {
                    return new
                    {
                        Type = package.Attributes["Type"]?.Value,
                        Architecture = package.Attributes["Architecture"]?.Value,
                        FileName = package.Attributes["FileName"]?.Value,
                        Version = package.Attributes["Version"]?.Value
                    };
                });

                var package = packages
                    .Where(p => p.Type == "application" && p.Architecture == arch)
                    .FirstOrDefault() ?? packages.FirstOrDefault();

                if (package != null)
                {
                    appxReader = new AppxReader(zip.GetInputStream(zip.GetEntry(package.FileName)));
                }
            }
        }
    }
}

file static class LinqExtension
{
    public static IEnumerable<dynamic> Select(this XmlNodeList nodes, Func<XmlNode, dynamic> selector)
    {
        foreach (XmlNode node in nodes)
        {
            yield return selector(node);
        }
    }
}
