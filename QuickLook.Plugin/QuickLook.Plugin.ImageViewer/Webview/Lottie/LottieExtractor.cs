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

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace QuickLook.Plugin.ImageViewer.Webview.Lottie;

internal static class LottieExtractor
{
    public static string GetJsonContent(string path)
    {
        using var fileStream = File.OpenRead(path);
        using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read);

        var manifestEntry = zipArchive.GetEntry("manifest.json");
        List<string> idEntries = [];

        if (manifestEntry != null)
        {
            using var manifestStream = manifestEntry.Open();
            using var manifestReader = new StreamReader(manifestStream, Encoding.UTF8);
            string content = manifestReader.ReadToEnd();

            if (!string.IsNullOrEmpty(content))
            {
                var manifestJson = LottieParser.Parse<Dictionary<string, object>>(content);

                if (manifestJson.ContainsKey("animations"))
                {
                    object animations = manifestJson["animations"];

                    if (manifestJson["animations"] is IEnumerable<object> animationsEnumerable)
                    {
                        foreach (var animationsItem in animationsEnumerable.ToArray())
                        {
                            if (animationsItem is Dictionary<string, object> animationsItemDict)
                            {
                                if (animationsItemDict.ContainsKey("id"))
                                {
                                    idEntries.Add($"animations/{animationsItemDict["id"]}");
                                }
                            }
                        }
                    }
                }

                // Read animations error from manifest.json and fallback to read all entries
                if (idEntries.Count == 0)
                {
                    foreach (var entry in zipArchive.Entries)
                    {
                        if (entry.FullName.StartsWith("animations"))
                        {
                            idEntries.Add(entry.FullName);
                        }
                    }
                }

                // Read the all animations
                if (idEntries.Count > 0)
                {
                    // I don't know if there are multiple animations
                    // But only support the first animation
                    var idEntry = $"{idEntries[0]}.json";
                    var animationEntry = zipArchive.GetEntry(idEntry);

                    if (animationEntry != null)
                    {
                        using var jsonStream = animationEntry.Open();
                        using var jsonReader = new StreamReader(jsonStream, Encoding.UTF8);
                        return jsonReader.ReadToEnd();
                    }
                }
            }
        }

        return null;
    }
}
