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

using DiscUtils.SquashFs;
using DiscUtils.Streams;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace QuickLook.Plugin.AppViewer.PackageParsers.AppImage;

public class AppImageReader
{
    public Dictionary<string, Dictionary<string, string>> DesktopEntry { get; private set; } = [];

    public string Arch => DesktopEntry["Desktop Entry"]["X-AppImage-Arch"];

    public string Version => DesktopEntry["Desktop Entry"]["X-AppImage-Version"];

    public string Name => DesktopEntry["Desktop Entry"]["X-AppImage-Name"];

    public string Exec => DesktopEntry["Desktop Entry"]["Exec"];

    public string Icon => DesktopEntry["Desktop Entry"]["Icon"];

    public Bitmap Logo { get; set; }

    public string Type => DesktopEntry["Desktop Entry"]["Type"];

    public string Terminal => DesktopEntry["Desktop Entry"]["Terminal"];

    public string[] Env { get; set; }

    static AppImageReader()
    {
        DiscUtils.Complete.SetupHelper.SetupComplete();
    }

    public AppImageReader(Stream stream)
    {
        Open(stream);
    }

    public AppImageReader(string path)
    {
        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        Open(fileStream);
    }

    private void Open(Stream stream)
    {
        using SquashFileSystemReader squash = FindSquashFsOffset(stream);
        ReadFiles(squash);
    }

    private SquashFileSystemReader FindSquashFsOffset(Stream stream)
    {
        byte[] buffer = new byte[4];

        for (long i = 0; i < stream.Length - 4; i++)
        {
            stream.Position = i;
            stream.ReadExactly(buffer, 0, 4);

            uint magic = BitConverter.ToUInt32(buffer, 0); // little-endian

            if (magic == SuperBlock.SquashFsMagic)
            {
                try
                {
                    var subStream = new SubStream(stream, i, stream.Length - i);
                    SuperBlock superBlock = new();
                    superBlock.ReadFrom(subStream, superBlock.Size);

                    // Supported for ZLib and Xz only
                    if (superBlock.Compression == SquashFileSystemCompressionKind.ZLib
                     || superBlock.Compression == SquashFileSystemCompressionKind.Xz)
                    {
                        subStream.Position = 0;
                        var squash = new SquashFileSystemReader(subStream);
                        return squash;
                    }
                    else if (Enum.IsDefined(typeof(SquashFileSystemCompressionKind), superBlock.Compression)
                          && superBlock.Compression != SquashFileSystemCompressionKind.Unknown)
                    {
                        // Unsupported compression
                        return null;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        return null;
    }

    private void ReadFiles(SquashFileSystemReader squash)
    {
        byte[] icon = null;
        Dictionary<string, byte[]> prepareIcons = [];

        foreach (var entry in squash.GetFileSystemEntries(@"\"))
        {
            try
            {
                Console.WriteLine(entry);

                if (entry == @"\.DirIcon")
                    continue; // Ignore symlink

                // Cache possible icon files in advance
                if (entry.EndsWith(".png"))
                {
                    prepareIcons.Add(entry, squash.ReadBytes(entry));
                }

                if (entry.EndsWith("AppRun.env"))
                {
                    string env = squash.ReadString(entry);
                    Env = env?.Split('\n').Where(e => !string.IsNullOrWhiteSpace(e)).ToArray() ?? [];
                }

                if (entry.EndsWith(".desktop"))
                {
                    string desktop = squash.ReadString(entry);
                    DesktopEntry = IniReader.Parse(desktop);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        if (DesktopEntry.ContainsKey("Desktop Entry"))
        {
            var section = DesktopEntry["Desktop Entry"];

            // Icon Lookup but PNG supported only
            // https://specifications.freedesktop.org/icon-theme-spec
            if (section.ContainsKey("Icon"))
            {
                string iconEntry = section["Icon"];

                if (prepareIcons.ContainsKey(@$"\{iconEntry}.png"))
                {
                    icon = prepareIcons[@$"\{iconEntry}.png"];
                }

                if (icon == null)
                {
                    foreach (var entry in squash.GetFileSystemEntries(@$"\usr\share\icons\hicolor\128x128\apps"))
                    {
                        try
                        {
                            if (entry == @$"\usr\share\icons\hicolor\128x128\apps\{iconEntry}.png")
                            {
                                prepareIcons.Add(entry, squash.ReadBytes(entry));
                                icon = prepareIcons[@$"\{iconEntry}.png"];
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                    }
                }

                if (icon != null)
                {
                    using MemoryStream ms = new(icon);
                    Logo = new Bitmap(ms);
                }
            }
        }
    }
}

file static class StreamExtension
{
    public static byte[] ReadBytes(this SquashFileSystemReader squash, string path)
    {
        using Stream s = squash.OpenFile(path, FileMode.Open);
        using MemoryStream ms = new();
        s.CopyTo(ms);
        byte[] data = ms.ToArray();
        return data;
    }

    public static string ReadString(this SquashFileSystemReader squash, string path, Encoding encoding = null)
    {
        return (encoding ?? Encoding.UTF8).GetString(squash.ReadBytes(path));
    }
}

file static class IniReader
{
    public static Dictionary<string, Dictionary<string, string>> Parse(string iniContent)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> currentSection = null;

        using (var reader = new StringReader(iniContent))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var sectionName = line.Substring(1, line.Length - 2).Trim();
                    currentSection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    result[sectionName] = currentSection;
                }
                else if (currentSection != null)
                {
                    var separatorIndex = line.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        var key = line.Substring(0, separatorIndex).Trim();
                        var value = line.Substring(separatorIndex + 1).Trim();
                        currentSection[key] = value;
                    }
                }
            }
        }

        return result;
    }
}
