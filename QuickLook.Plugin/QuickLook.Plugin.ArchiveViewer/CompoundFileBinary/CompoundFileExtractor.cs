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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace QuickLook.Plugin.ArchiveViewer.CompoundFileBinary;

/// <summary>
/// Utility class to extract streams and storages from a COM compound file (IStorage) into the file system.
/// This is a thin managed wrapper that enumerates entries inside the compound file and writes streams to disk.
/// </summary>
public static partial class CompoundFileExtractor
{
    /// <summary>
    /// Extracts all streams and storages from the compound file at <paramref name="compoundFilePath"/>
    /// into the specified <paramref name="destinationDirectory"/>. Directory structure inside the compound
    /// file is preserved.
    /// </summary>
    /// <param name="compoundFilePath">Path to the compound file (OLE compound file / structured storage).</param>
    /// <param name="destinationDirectory">Destination directory to write extracted files and directories to. If it does not exist it will be created.</param>
    public static void ExtractToDirectory(string compoundFilePath, string destinationDirectory)
    {
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        // Ensure the compound file exists
        if (!File.Exists(compoundFilePath))
            throw new FileNotFoundException("Compound file not found.", compoundFilePath);

        // Validate magic header for OLE compound file: D0 CF 11 E0 A1 B1 1A E1
        byte[] magicHeader = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
        byte[] header = new byte[8];
        using (FileStream fs = new(compoundFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            int read = fs.Read(header, 0, header.Length);
            if (read < header.Length || !header.SequenceEqual(magicHeader))
            {
                throw new InvalidDataException("The specified file does not appear to be an OLE Compound File (invalid header).");
            }
        }

        // Open the compound file as an IStorage implementation wrapped by DisposableIStorage.
        using DisposableIStorage storage = new(compoundFilePath, STGM.DIRECT | STGM.READ | STGM.SHARE_EXCLUSIVE, IntPtr.Zero);
        IEnumerator<STATSTG> enumerator = storage.EnumElements();

        // Enumerate all elements (streams and storages) at the root of the compound file.
        while (enumerator.MoveNext())
        {
            STATSTG entryStat = enumerator.Current;

            // STGTY_STREAM indicates the element is a stream (treat as a file).
            if (entryStat.type == (int)STGTY.STGTY_STREAM)
            {
                ExtractStreamToDirectory(storage, entryStat.pwcsName, destinationDirectory);
            }
            // STGTY_STORAGE indicates the element is a nested storage (treat as a directory).
            else if (entryStat.type == (int)STGTY.STGTY_STORAGE)
            {
                ExtractStorageToDirectory(storage, entryStat.pwcsName, destinationDirectory);
            }
        }
    }

    /// <summary>
    /// Extracts a single stream from the provided <paramref name="storage"/> and writes it to <paramref name="destinationDirectory"/>.
    /// </summary>
    /// <param name="storage">The parent storage that contains the stream.</param>
    /// <param name="entryName">Name of the stream inside the compound file.</param>
    /// <param name="destinationDirectory">Directory to write the extracted stream to.</param>
    private static void ExtractStreamToDirectory(DisposableIStorage storage, string entryName, string destinationDirectory)
    {
        // Build target file path for the stream extraction.
        string outputPath = Path.Combine(destinationDirectory, entryName);

        // Open the stream for reading from the compound file.
        using DisposableIStream stream = storage.OpenStream(entryName, IntPtr.Zero, STGM.READ | STGM.SHARE_EXCLUSIVE);

        // Query stream statistics to determine its size.
        STATSTG streamStat = stream.Stat((int)STATFLAG.STATFLAG_DEFAULT);

        // Allocate a buffer exactly the size of the stream and read it in one call.
        // Note: cbSize is an unsigned 64-bit value in the native struct; the wrapper exposes it as an int/long depending on implementation.
        byte[] buffer = new byte[streamStat.cbSize];
        stream.Read(buffer, buffer.Length);

        // Write the stream contents to disk. This will overwrite existing files.
        File.WriteAllBytes(outputPath, buffer);
    }

    /// <summary>
    /// Extracts a nested storage (directory) recursively. Creates a corresponding directory on disk and
    /// extracts its child streams and storages.
    /// </summary>
    /// <param name="storage">The parent storage that contains the nested storage.</param>
    /// <param name="entryName">Name of the nested storage inside the compound file.</param>
    /// <param name="parentDirectory">Directory on disk that will contain the created directory for this nested storage.</param>
    private static void ExtractStorageToDirectory(DisposableIStorage storage, string entryName, string parentDirectory)
    {
        string currentDirectory = Path.Combine(parentDirectory, entryName);
        if (!Directory.Exists(currentDirectory))
        {
            Directory.CreateDirectory(currentDirectory);
        }

        // Open the nested storage and enumerate its elements recursively.
        using DisposableIStorage subStorage = storage.OpenStorage(entryName, null, STGM.READ | STGM.SHARE_EXCLUSIVE, IntPtr.Zero);
        IEnumerator<STATSTG> enumerator = subStorage.EnumElements();
        while (enumerator.MoveNext())
        {
            STATSTG entryStat = enumerator.Current;

            // STGTY_STREAM indicates the element is a stream (treat as a file).
            if (entryStat.type == (int)STGTY.STGTY_STREAM)
            {
                ExtractStreamToDirectory(subStorage, entryStat.pwcsName, currentDirectory);
            }
            // STGTY_STORAGE indicates the element is a nested storage (treat as a directory).
            else if (entryStat.type == (int)STGTY.STGTY_STORAGE)
            {
                ExtractStorageToDirectory(subStorage, entryStat.pwcsName, currentDirectory);
            }
        }
    }
}

/// <summary>
/// Utility class to extract streams and storages from a COM compound file (IStorage) into the memory.
/// This is a thin managed wrapper that enumerates entries inside the compound file and writes streams to dictionary.
/// </summary>
public static partial class CompoundFileExtractor
{
    /// <summary>
    /// Extracts all streams from the compound file at <paramref name="compoundFilePath"/>
    /// into a dictionary where the key is the relative path and the value is the file content.
    /// </summary>
    /// <param name="compoundFilePath">Path to the compound file (OLE compound file / structured storage).</param>
    /// <returns>A dictionary containing the extracted files.</returns>
    public static Dictionary<string, byte[]> ExtractToDictionary(string compoundFilePath)
    {
        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        // Ensure the compound file exists
        if (!File.Exists(compoundFilePath))
            throw new FileNotFoundException("Compound file not found.", compoundFilePath);

        // Validate magic header for OLE compound file: D0 CF 11 E0 A1 B1 1A E1
        byte[] magicHeader = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
        byte[] header = new byte[8];
        using (FileStream fs = new(compoundFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            int read = fs.Read(header, 0, header.Length);
            if (read < header.Length || !header.SequenceEqual(magicHeader))
            {
                throw new InvalidDataException("The specified file does not appear to be an OLE Compound File (invalid header).");
            }
        }

        // Open the compound file as an IStorage implementation wrapped by DisposableIStorage.
        using DisposableIStorage storage = new(compoundFilePath, STGM.DIRECT | STGM.READ | STGM.SHARE_EXCLUSIVE, IntPtr.Zero);
        ExtractStorageToDictionary(storage, string.Empty, result);

        return result;
    }

    private static void ExtractStorageToDictionary(DisposableIStorage storage, string currentPath, Dictionary<string, byte[]> result)
    {
        IEnumerator<STATSTG> enumerator = storage.EnumElements();

        // Enumerate all elements (streams and storages) at the root of the compound file.
        while (enumerator.MoveNext())
        {
            STATSTG entryStat = enumerator.Current;
            string entryPath = string.IsNullOrEmpty(currentPath) ? entryStat.pwcsName : Path.Combine(currentPath, entryStat.pwcsName);

            // STGTY_STREAM indicates the element is a stream (treat as a file).
            if (entryStat.type == (int)STGTY.STGTY_STREAM)
            {
                // Open the stream for reading from the compound file.
                using DisposableIStream stream = storage.OpenStream(entryStat.pwcsName, IntPtr.Zero, STGM.READ | STGM.SHARE_EXCLUSIVE);

                // Query stream statistics to determine its size.
                STATSTG streamStat = stream.Stat((int)STATFLAG.STATFLAG_DEFAULT);

                // Allocate a buffer exactly the size of the stream and read it in one call.
                byte[] buffer = new byte[streamStat.cbSize];
                stream.Read(buffer, buffer.Length);

                result[entryPath] = buffer;
            }
            // STGTY_STORAGE indicates the element is a nested storage (treat as a directory).
            else if (entryStat.type == (int)STGTY.STGTY_STORAGE)
            {
                using DisposableIStorage subStorage = storage.OpenStorage(entryStat.pwcsName, null, STGM.READ | STGM.SHARE_EXCLUSIVE, IntPtr.Zero);
                ExtractStorageToDictionary(subStorage, entryPath, result);
            }
        }
    }
}
