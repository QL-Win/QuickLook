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

using DiscUtils;
using DiscUtils.HfsPlus;
using QuickLook.Common.Helpers;
using QuickLook.Plugin.AppViewer.PackageParsers.Ipa;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Dmg;

public class DmgReader : IDisposable
{
    public string VolumeLabel { get; set; }

    public string ContentsEntry { get; set; }

    public Dictionary<string, DmgArchive> Archives { get; } = [];

    public Dictionary<string, object> InfoPlistDict { get; set; } = [];

    public string DisplayName { get; set; }

    public string ShortVersionString { get; set; }

    public string Version { get; set; }

    public string Identifier { get; set; }

    public byte[] Icon { get; set; }

    public string IconName { get; set; }

    public string IconEntry { get; set; }

    public Bitmap Logo { get; set; }

    public string MinimumOSVersion { get; set; }

    public string PlatformVersion { get; set; }

    public string SupportedPlatforms { get; set; }

    static DmgReader()
    {
        DiscUtils.Complete.SetupHelper.SetupComplete();
    }

    public DmgReader(string path)
    {
        Open(path);
    }

    public void Dispose()
    {
        if (Archives is not null)
        {
            foreach (var archive in Archives.Values)
            {
                archive.Dispose();
            }
            Archives.Clear();
        }
    }

    private void Open(string path)
    {
        using var disk = VirtualDisk.OpenDisk(path, FileAccess.Read, useAsync: false);
        if (disk is null)
        {
            Debug.WriteLine($"Failed to open '{path}' as virtual disk.");
            return;
        }

        try
        {
            // Find the first (and supposedly, only, HFS partition)
            foreach (var volume in VolumeManager.GetPhysicalVolumes(disk))
            {
                foreach (var fileSystem in FileSystemManager.DetectFileSystems(volume))
                {
                    // Apple HFS+
                    if (fileSystem.Name == "HFS+")
                    {
                        using var hfs = (HfsPlusFileSystem)fileSystem.Open(volume);

                        VolumeLabel = hfs.VolumeLabel;
                        ListFiles(hfs, string.Empty);
                    }
                }
            }

            byte[] infoPlistData = null;

            foreach (var archive in Archives.Values)
            {
                Match m = Regex.Match(archive.Entry, @".*\.app\\Contents\\Info\.plist$");

                if (m.Success)
                {
                    ContentsEntry = Path.GetDirectoryName(archive.Entry);
                    infoPlistData = archive.GetBytes();
                    if (Plist.ReadPlist(infoPlistData) is Dictionary<string, object> dict)
                    {
                        InfoPlistDict = dict;
                    }
                    break;
                }
            }
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
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
            if (InfoPlistDict.TryGetValue("LSMinimumSystemVersion", out object value) && value is string stringValue)
            {
                MinimumOSVersion = $"macOS {stringValue}";
            }
        }
        {
            if (InfoPlistDict.TryGetValue("DTPlatformVersion", out object value) && value is string stringValue)
            {
                PlatformVersion = $"macOS {stringValue}";
            }
        }
        {
            if (InfoPlistDict.TryGetValue("CFBundleSupportedPlatforms", out object familyNode) && familyNode is IEnumerable<object> list)
            {
                SupportedPlatforms = string.Join(", ", list);
            }
        }
        {
            if (InfoPlistDict.TryGetValue("CFBundleIconFile", out object iconFilesNode) && iconFilesNode is object iconFile)
            {
                IconName = iconFile as string;
            }
        }
        {
            if (!string.IsNullOrWhiteSpace(IconName))
            {
                foreach (var archive in Archives.Values)
                {
                    if (archive.Entry.StartsWith($@"{ContentsEntry}\Resources\{IconName}."))
                    {
                        IconEntry = archive.Entry;
                        Icon = archive.GetBytes();

                        if (Path.GetExtension(IconEntry).ToLower() == ".icns")
                        {
                            Logo = IcnsParser.Parse(Icon);
                        }
                        break;
                    }
                }
            }
        }
    }

    private void ListFiles(HfsPlusFileSystem fs, string path)
    {
        foreach (var entry in fs.GetFileSystemEntries(path))
        {
            Debug.WriteLine(entry);

            if (fs.DirectoryExists(entry))
            {
                ListFiles(fs, entry);
            }
            else if (fs.FileExists(entry))
            {
                Archives.Add(entry, new DmgArchive()
                {
                    Entry = entry,
                    FileSystem = fs,
                });
            }
        }
    }
}
