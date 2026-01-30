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

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuickLook.Plugin.ArchiveViewer.CompoundFileBinary;

/// <summary>
/// Utility to extract contents from an EIF archive and optionally reorder images
/// based on metadata stored in Face.dat. The EIF format is a Compound File Binary
/// (structured storage) used by QQ for emoji packs.
/// </summary>
public static class EifExtractor
{
    /// <summary>
    /// File name of the Face.dat metadata stream inside EIF archives.
    /// Face.dat contains mapping information used to order and rename images.
    /// </summary>
    public const string FaceDat = "Face.dat";

    /// <summary>
    /// Extracts files from the compound file at <paramref name="path"/> into
    /// <paramref name="outputDirectory"/>. If Face.dat exists inside the archive,
    /// images will be renamed and reordered according to the mapping in Face.dat.
    /// </summary>
    /// <param name="path">Path to the EIF compound file.</param>
    /// <param name="outputDirectory">Destination directory to write extracted files.</param>
    public static void ExtractToDirectory(string path, string outputDirectory)
    {
        // Extract all streams from the compound file into an in-memory dictionary
        Dictionary<string, byte[]> compoundFile = CompoundFileExtractor.ExtractToDictionary(path);

        // If Face.dat exists, build mapping and reorder images accordingly
        if (compoundFile.ContainsKey(FaceDat))
        {
            // Build group -> (filename -> index) mapping from Face.dat
            Dictionary<string, Dictionary<string, int>> faceDat = FaceDatDecoder.Decode(compoundFile[FaceDat]);

            // Flatten mapping to key '\\' joined: "group\filename" -> index
            Dictionary<string, int> faceDatMapper = faceDat.SelectMany(
                outer => outer.Value,
                (outer, inner) => new { Key = $@"{outer.Key}\{inner.Key}", inner.Value })
                .ToDictionary(x => x.Key, x => x.Value);

            // Prepare output dictionary for files that match mapping
            Dictionary<string, byte[]> output = [];

            foreach (var kv in faceDatMapper)
            {
                if (compoundFile.ContainsKey(kv.Key))
                {
                    // Create a new key using the index as file name and keep original extension
                    string newKey = Path.Combine(Path.GetDirectoryName(kv.Key),
                        faceDatMapper[kv.Key] + Path.GetExtension(kv.Key));

                    output[newKey] = compoundFile[kv.Key];
                }
            }

            // Ensure target directory exists
            Directory.CreateDirectory(outputDirectory);

            // Write each matched file to disk using its new name
            foreach (var kv in output)
            {
                (string relativePath, byte[] data) = (kv.Key, kv.Value);
                string fullPath = Path.Combine(outputDirectory, relativePath);

                // Ensure parent directory exists
                string dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                // Write file bytes
                File.WriteAllBytes(fullPath, data);
            }
        }
    }
}
