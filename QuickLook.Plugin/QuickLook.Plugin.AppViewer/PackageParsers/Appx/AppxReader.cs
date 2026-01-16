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
using System.Text.RegularExpressions;
using System.Xml;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Appx;

public class AppxReader : IDisposable
{
    private ZipFile zip;

    public string Name { get; set; }
    public string Publisher { get; set; }
    public string Version { get; set; }
    public string DisplayName { get; set; }
    public string PublisherDisplayName { get; set; }
    public string Description { get; set; }
    public string Logo { get; set; }
    public string[] Capabilities { get; set; }

    public Bitmap Icon
    {
        get
        {
            if (string.IsNullOrEmpty(Logo)) return null;

            string extension = Path.GetExtension(Logo);
            string name = Logo.Substring(0, Logo.Length - extension.Length);

            ZipEntry logoEntry = null;
            int logoScale = 0;

            foreach (ZipEntry entry in zip)
            {
                Match m = Regex.Match(entry.Name, @$"{name}(\.scale\-(\d+))?{extension}");

                if (m.Success)
                {
                    if (int.TryParse(m.Groups[2].Value, out int currentScale))
                    {
                        if (currentScale > logoScale)
                        {
                            logoEntry = entry;
                            logoScale = currentScale;
                        }
                    }
                }
            }

            if (logoEntry != null)
            {
                return new Bitmap(zip.GetInputStream(logoEntry));
            }
            return null;
        }
    }

    public AppxReader(Stream stream)
    {
        zip = new ZipFile(stream);
        Open();
    }

    public AppxReader(string path)
    {
        zip = new ZipFile(path);
        Open();
    }

    public void Dispose()
    {
        zip?.Close();
        zip = null;
    }

    private void Open()
    {
        ZipEntry entry = zip.GetEntry("AppxManifest.xml")
            ?? throw new InvalidDataException("AppxManifest.xml not found");

        XmlDocument xml = new() { XmlResolver = null };
        xml.Load(zip.GetInputStream(entry));

        XmlElement packageNode = xml.DocumentElement;

        // Identity
        {
            XmlElement identityNode = packageNode["Identity"];

            if (identityNode != null)
            {
                Name = identityNode.Attributes["Name"]?.Value;
                Version = identityNode.Attributes["Version"]?.Value;
                Publisher = identityNode.Attributes["Publisher"]?.Value;

                Match m = Regex.Match(Publisher, @"CN=([^,]*),?");
                if (m.Success) Publisher = m.Groups[1].Value;
            }
        }

        // Properties
        {
            XmlElement propertiesNode = packageNode["Properties"];

            if (propertiesNode != null)
            {
                DisplayName = propertiesNode["DisplayName"]?.FirstChild?.Value;
                PublisherDisplayName = propertiesNode["PublisherDisplayName"]?.FirstChild?.Value;
                Description = propertiesNode["Description"]?.FirstChild?.Value;
                Logo = propertiesNode["Logo"]?.FirstChild?.Value.Replace(@"\", "/");
            }
        }

        // Capabilities
        {
            XmlElement capabilitiesNode = packageNode["Capabilities"];

            if (capabilitiesNode != null)
            {
                Capabilities = [.. capabilitiesNode.ChildNodes.Select(capability => capability.Attributes["Name"]?.Value)];
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
