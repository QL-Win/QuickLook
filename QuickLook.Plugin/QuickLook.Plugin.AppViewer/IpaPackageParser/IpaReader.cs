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

using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuickLook.Plugin.AppViewer.IpaPackageParser;

public class IpaReader
{
    private ZipFile zip;
    private string appRoot;

    public Dictionary<string, object> InfoPlistDict { get; set; }

    public Dictionary<string, object> ItunesMetadataDic { get; set; }

    public string DisplayName { get; set; }

    public string ShortVersionString { get; set; }

    public string Version { get; set; }

    public string Identifier { get; set; }

    public byte[] Icon { get; set; }

    public string IconName { get; set; }

    public string DeviceFamily { get; set; }

    public string MinimumOSVersion { get; set; }

    public string PlatformVersion { get; set; }

    public IpaReader(string path)
    {
        Open(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    public IpaReader(Stream stream)
    {
        Open(stream);
    }

    protected void Dispose()
    {
        zip?.Close();
        zip = null;
    }

    private void Open(Stream stream)
    {
        zip = new ZipFile(stream);
        byte[] infoPlistData = null;

        // Info.plist
        {
            foreach (ZipEntry entry in zip)
            {
                Match m = Regex.Match(entry.Name, @"(Payload/.*\.app/)Info\.plist");

                if (m.Success)
                {
                    appRoot = m.Groups[1].Value;
                    ZipEntry infoPlist = zip.GetEntry(appRoot);
                    using var s = new BinaryReader(zip.GetInputStream(entry));
                    infoPlistData = s.ReadBytes((int)entry.Size);
                    break;
                }
            }
            if (Plist.ReadPlist(infoPlistData) is Dictionary<string, object> dict)
            {
                InfoPlistDict = dict;
            }
        }
        {
            if (InfoPlistDict.TryGetValue("CFBundleDisplayName", out object value) && value is string stringValue)
            {
                DisplayName = stringValue;
            }
        }
        {
            if (InfoPlistDict.TryGetValue("CFBundleShortVersionString", out object value) && value is string stringValue)
            {
                ShortVersionString = stringValue;
            }
        }
        {
            if (InfoPlistDict.TryGetValue("CFBundleVersion", out object value) && value is string stringValue)
            {
                Version = stringValue;
            }
        }
        {
            if (InfoPlistDict.TryGetValue("CFBundleIdentifier", out object value) && value is string stringValue)
            {
                Identifier = stringValue;
            }
        }
        {
            if (InfoPlistDict.TryGetValue("MinimumOSVersion", out object value) && value is string stringValue)
            {
                MinimumOSVersion = $"iOS {stringValue}";
            }
        }
        {
            if (InfoPlistDict.TryGetValue("DTPlatformVersion", out object value) && value is string stringValue)
            {
                PlatformVersion = $"iOS {stringValue}";
            }
        }
        {
            if (InfoPlistDict.TryGetValue("UIDeviceFamily", out object familyNode) && familyNode is IEnumerable<object> list)
            {
                DeviceFamily = string.Join(", ",
                    list.Select(deviceId => Convert.ToInt32(deviceId) switch
                    {
                        1 => "iPhone",
                        2 => "iPad",
                        3 => "Apple TV",
                        4 => "Apple Watch",
                        5 => "HomePod",
                        6 => "Mac",
                        7 => "Apple Vision Pro",
                        _ => "Unknown Device"
                    })
                );
            }
        }

        {
            if (InfoPlistDict.TryGetValue("CFBundleIcons", out object iconsNode) && iconsNode is IDictionary<string, object> icons)
            {
                if (icons.TryGetValue("CFBundlePrimaryIcon", out object primaryIconsNode) && primaryIconsNode is IDictionary<string, object> primaryIcons)
                {
                    if (primaryIcons.TryGetValue("CFBundleIconFiles", out object iconFilesNode) && iconFilesNode is IList<object> iconFiles)
                    {
                        IconName = iconFiles.LastOrDefault() as string;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(IconName))
            {
                if (InfoPlistDict.TryGetValue("CFBundleIconFiles", out object iconFilesNode) && iconFilesNode is IList<object> iconFiles)
                {
                    IconName = iconFiles.LastOrDefault() as string;
                }
            }
            if (!string.IsNullOrWhiteSpace(IconName))
            {
                foreach (ZipEntry entry in zip)
                {
                    if (entry.Name.StartsWith(appRoot))
                    {
                        string fileName = Path.GetFileName(entry.Name);

                        if (fileName.StartsWith(IconName))
                        {
                            using var s = new BinaryReader(zip.GetInputStream(entry));
                            Icon = s.ReadBytes((int)entry.Size);
                            break;
                        }
                    }
                }
            }
        }
    }
}
