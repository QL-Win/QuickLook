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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QuickLook.Plugin.TextViewer.Detectors;

/// <summary>
/// Parses Windows OLE Compound File Binary (CFB) Thumbs.db files and returns a
/// plain-text list of the embedded thumbnail file names, following the same
/// output style as <see cref="DSStoreDetector"/>.
///
/// Parser logic is ported from the open-source Thumbs Viewer project
/// (https://thumbsviewer.github.io/) by Eric Kutcher.
/// </summary>
public sealed class ThumbsDbDetector : ITransferFormatDetector
{
    public string Name => "YAML";

    public string Extension => ".yaml";

    public string RealExtension => "Thumbs.db";

    public bool Detect(string path, string text)
    {
        _ = text;
        if (string.IsNullOrEmpty(path)) return false;
        return Path.GetFileName(path).Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase);
    }

    public string Transfer(string path)
    {
        if (!Detect(path, null)) return null;

        try
        {
            var data = File.ReadAllBytes(path);
            var entries = ParseThumbsDb(data);

            var sb = new StringBuilder();
            sb.AppendLine($"Thumbs.db: {Path.GetFileName(path)}");
            sb.AppendLine($"Entries: {entries.Count}");
            sb.AppendLine();
            foreach (var e in entries)
                sb.AppendLine(e);
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Failed to parse Thumbs.db: {ex.Message}";
        }
    }

    private static readonly byte[] OleMagic = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private static List<string> ParseThumbsDb(byte[] data)
    {
        if (data.Length < 512)
            throw new InvalidDataException("File too small to be a valid OLE Compound Document");

        for (int i = 0; i < OleMagic.Length; i++)
            if (data[i] != OleMagic[i])
                throw new InvalidDataException("Not a valid OLE Compound Document (bad magic)");

        // ── Header fields (all little-endian) ──
        // Offsets follow the database_header struct in read_thumbs.h
        ushort sectorShift = ReadUInt16LE(data, 30);        // 9 → 512 B/sector, 12 → 4096 B/sector
        int numSatSectors = (int)ReadUInt32LE(data, 44);
        int firstDirSector = ReadInt32LE(data, 48);
        uint shortSectorCutoff = ReadUInt32LE(data, 56);    // normally 0x1000
        int firstSsatSector = ReadInt32LE(data, 60);
        int numSsatSectors = (int)ReadUInt32LE(data, 64);
        int firstDifatSector = ReadInt32LE(data, 68);
        int numDifatSectors = (int)ReadUInt32LE(data, 72);

        int sectSize = 1 << sectorShift;                    // 512 or 4096

        // ── Build SAT via DIFAT ──
        var difat = new List<int>(109);
        for (int i = 0; i < 109; i++)
        {
            int idx = ReadInt32LE(data, 76 + i * 4);
            if (idx < 0) break;                             // -1 = free/unused
            difat.Add(idx);
        }

        // Follow extended DIFAT chain
        if (numDifatSectors > 0 && firstDifatSector >= 0)
        {
            int difatSect = firstDifatSector;
            while (difatSect >= 0)
            {
                int sectorBase = 512 + difatSect * sectSize;
                int entriesPerSect = sectSize / 4 - 1;      // last slot is next-DIFAT pointer
                for (int i = 0; i < entriesPerSect; i++)
                {
                    int idx = ReadInt32LE(data, sectorBase + i * 4);
                    if (idx < 0) break;
                    difat.Add(idx);
                }
                difatSect = ReadInt32LE(data, sectorBase + entriesPerSect * 4);
            }
        }

        int satCapacity = Math.Max(numSatSectors, difat.Count) * (sectSize / 4);
        var sat = new int[satCapacity];
        for (int i = 0; i < sat.Length; i++) sat[i] = -1;

        for (int di = 0; di < difat.Count; di++)
        {
            int satSector = difat[di];
            if (satSector < 0) continue;
            int sectorBase = 512 + satSector * sectSize;
            int baseIdx = di * (sectSize / 4);
            for (int i = 0; i < sectSize / 4 && baseIdx + i < sat.Length; i++)
            {
                int fileOff = sectorBase + i * 4;
                if (fileOff + 4 > data.Length) break;
                sat[baseIdx + i] = ReadInt32LE(data, fileOff);
            }
        }

        // ── Build SSAT ──
        int[] ssat = null;
        if (numSsatSectors > 0 && firstSsatSector >= 0)
        {
            int ssatCapacity = numSsatSectors * (sectSize / 4);
            ssat = new int[ssatCapacity];
            for (int i = 0; i < ssat.Length; i++) ssat[i] = -1;
            int ssatSect = firstSsatSector;
            int ssatIdx = 0;
            while (ssatSect >= 0 && ssatIdx < ssatCapacity)
            {
                int sectorBase = 512 + ssatSect * sectSize;
                for (int i = 0; i < sectSize / 4 && ssatIdx < ssatCapacity; i++)
                {
                    int fileOff = sectorBase + i * 4;
                    if (fileOff + 4 > data.Length) break;
                    ssat[ssatIdx++] = ReadInt32LE(data, fileOff);
                }
                if (ssatSect < sat.Length)
                    ssatSect = sat[ssatSect];
                else
                    break;
            }
        }

        // ── Read directory entries ──
        var dirEntries = new List<DirEntry>();
        int dirSect = firstDirSector;
        int visited = 0;
        while (dirSect >= 0 && visited++ < sat.Length)
        {
            int sectorBase = 512 + dirSect * sectSize;
            int entriesPerSect = sectSize / 128;
            for (int i = 0; i < entriesPerSect; i++)
            {
                int entryOff = sectorBase + i * 128;
                if (entryOff + 128 > data.Length) break;
                var e = ReadDirEntry(data, entryOff);
                if (e.EntryType != 0)
                    dirEntries.Add(e);
            }
            if (dirSect < sat.Length)
                dirSect = sat[dirSect];
            else
                break;
        }

        // ── Identify root, Catalog, and thumbnail stream entries ──
        DirEntry? rootEntry = null;
        DirEntry? catalogEntry = null;
        var streamEntries = new List<DirEntry>();

        foreach (var e in dirEntries)
        {
            if (e.EntryType == 5)
            {
                rootEntry = e;
            }
            else if (e.EntryType == 2 &&
                     e.Name.Equals("Catalog", StringComparison.OrdinalIgnoreCase))
            {
                catalogEntry = e;
            }
            else if (e.EntryType == 2)
            {
                streamEntries.Add(e);
            }
        }

        // ── Cache short stream container (lives in the root entry's SAT stream) ──
        byte[] shortContainer = null;
        if (rootEntry.HasValue &&
            rootEntry.Value.FirstSector >= 0 &&
            rootEntry.Value.Size > 0)
        {
            shortContainer = ReadSatStream(
                data, sat, rootEntry.Value.FirstSector, rootEntry.Value.Size, sectSize);
        }

        // Parse catalog if available (XP / Windows Me / 2000)
        if (catalogEntry.HasValue)
        {
            byte[] catData = ReadEntryStream(
                data, sat, ssat, shortContainer,
                catalogEntry.Value, shortSectorCutoff, sectSize, 64 /* shortSectSize */);
            return ParseCatalog(catData, sectSize);
        }

        // No catalog (Vista/7+): return numeric stream names
        var names = new List<string>(streamEntries.Count);
        foreach (var e in streamEntries)
            names.Add(e.Name);
        return names;
    }

    /// <summary>
    /// Parses the Catalog stream of a Thumbs.db file and returns the original
    /// file names that were thumbnailed.
    ///
    /// Catalog stream layout (all LE):
    ///   [0..1]  uint16  offset to first entry
    ///   [2..3]  uint16  version (1=WMC/XP, 4=Me/2000, 5/6/7=XP variants)
    ///   [offset..]  repeated entries:
    ///     uint32  entry_length  (includes all following fields + itself)
    ///     uint32  entry_number
    ///     int64   modified time (FILETIME)
    ///     uint32  padding       (only when sector_size == 4096; entry_length decremented)
    ///     wchar[] file name     (UTF-16 LE, length = entry_length − 0x14)
    ///     uint32  trailing
    /// </summary>
    private static List<string> ParseCatalog(byte[] data, int sectSize)
    {
        if (data == null || data.Length < 4)
            return [];

        int offset = ReadUInt16LE(data, 0); // usually 4
        bool isLargeSectors = sectSize == 4096;

        var results = new List<string>();
        while (offset + 16 <= data.Length)
        {
            uint entryLength = ReadUInt32LE(data, offset);
            if (entryLength < 0x14 || offset + entryLength > data.Length)
                break;

            offset += 4; // past entry_length field
            // entry_num
            offset += 4;
            // date_modified
            offset += 8;

            // Version-4 databases (sector size 4096) have an extra uint32 padding
            if (isLargeSectors)
            {
                offset += 4;
                entryLength -= 4;
            }

            int nameLength = (int)(entryLength - 0x14);
            if (nameLength <= 0 || offset + nameLength > data.Length)
                break;

            string name = Encoding.Unicode
                .GetString(data, offset, nameLength)
                .TrimEnd('\0');
            results.Add(name);

            offset += nameLength + 4; // past name + trailing uint32
        }
        return results;
    }

    /// <summary>Reads a stream stored in the regular SAT.</summary>
    private static byte[] ReadSatStream(byte[] data, int[] sat, int firstSector, uint size, int sectSize)
    {
        var buf = new byte[size];
        int satIdx = firstSector;
        int bytesRead = 0;
        while (satIdx >= 0 && satIdx < sat.Length && bytesRead < (int)size)
        {
            int sectorBase = 512 + satIdx * sectSize;
            int toRead = Math.Min(sectSize, (int)size - bytesRead);
            if (sectorBase + toRead > data.Length) break;
            Buffer.BlockCopy(data, sectorBase, buf, bytesRead, toRead);
            bytesRead += toRead;
            satIdx = sat[satIdx];
        }
        return buf;
    }

    /// <summary>
    /// Reads an entry's stream, choosing between the regular SAT and the short
    /// stream container based on <paramref name="shortSectorCutoff"/>.
    /// </summary>
    private static byte[] ReadEntryStream(
        byte[] data, int[] sat, int[] ssat, byte[] shortContainer,
        DirEntry entry, uint shortSectorCutoff, int sectSize, int shortSectSize)
    {
        if (entry.Size >= shortSectorCutoff || ssat == null || shortContainer == null)
            return ReadSatStream(data, sat, entry.FirstSector, entry.Size, sectSize);

        // Read from short stream container
        var buf = new byte[entry.Size];
        int ssatIdx = entry.FirstSector;
        int bytesRead = 0;
        while (ssatIdx >= 0 && ssatIdx < ssat.Length && bytesRead < (int)entry.Size)
        {
            int ssOffset = ssatIdx * shortSectSize;
            int toRead = Math.Min(shortSectSize, (int)entry.Size - bytesRead);
            if (ssOffset + toRead > shortContainer.Length) break;
            Buffer.BlockCopy(shortContainer, ssOffset, buf, bytesRead, toRead);
            bytesRead += toRead;
            ssatIdx = ssat[ssatIdx];
        }
        return buf;
    }

    /// <summary>
    /// Reads a 128-byte directory entry from the compound document directory.
    /// directory_header layout:
    ///   [0..63]    wchar_t[32]  entry name (NULL-terminated UTF-16 LE)
    ///   [64..65]   uint16       name length in bytes (including terminator)
    ///   [66]       byte         entry type (0=invalid,1=storage,2=stream,5=root)
    ///   [116..119] int32        first stream sector
    ///   [120..123] uint32       stream size (low 32 bits)
    /// </summary>
    private static DirEntry ReadDirEntry(byte[] data, int offset)
    {
        string name = Encoding.Unicode.GetString(data, offset, 62).TrimEnd('\0');
        byte entryType = data[offset + 66];
        int firstSector = ReadInt32LE(data, offset + 116);
        uint size = ReadUInt32LE(data, offset + 120);
        return new DirEntry(name, entryType, firstSector, size);
    }

    private static ushort ReadUInt16LE(byte[] d, int o) =>
        (ushort)(d[o] | (d[o + 1] << 8));

    private static uint ReadUInt32LE(byte[] d, int o) =>
        (uint)(d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24));

    private static int ReadInt32LE(byte[] d, int o) =>
        d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24);

    private readonly struct DirEntry(string name, byte entryType, int firstSector, uint size)
    {
        public string Name { get; } = name;
        public byte EntryType { get; } = entryType;
        public int FirstSector { get; } = firstSector;
        public uint Size { get; } = size;
    }
}
