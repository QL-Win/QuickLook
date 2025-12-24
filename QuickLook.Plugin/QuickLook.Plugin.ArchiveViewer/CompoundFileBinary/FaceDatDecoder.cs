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
using System.Text;

namespace QuickLook.Plugin.ArchiveViewer.CompoundFileBinary;

/// <summary>
/// Decoder for Face.dat entries used by QQ EIF packages.
/// Re-implements the behavior of the provided Python scripts:
/// - Finds special marker sequence <c>e_str_file_org</c> inside each line
/// - Skips 4 bytes after the marker (same as the Python implementation)
/// - Locates a repeating-key pattern and extracts the XOR-encrypted block
/// - XOR-decodes the block and parses group\filename entries
/// Provides a method to build the same group -> (filename -> index) mapping as the Python tool.
/// </summary>
public static class FaceDatDecoder
{
    /// <summary>
    /// Marker sequence used in the Python script
    /// </summary>
    private static readonly byte[] EStrFileOrg = [0x98, 0xEB, 0x9F, 0xEB, 0x99, 0xEB, 0xAD, 0xEB, 0x82, 0xEB, 0x87, 0xEB, 0x8E, 0xEB, 0x84, 0xEB, 0x99, 0xEB, 0x8C, 0xEB];

    /// <summary>
    /// Decode returns a mapping of group name to a dictionary mapping file name to index within the group.
    /// This matches the Python script's <c>group_dict</c> structure.
    /// </summary>
    /// <param name="fileBytes">The raw bytes of Face.dat.</param>
    /// <returns>Nested dictionary: group -> (filename -> index).</returns>
    public static Dictionary<string, Dictionary<string, int>> Decode(byte[] fileBytes)
    {
        return BuildGroupIndex(fileBytes);
    }

    /// <summary>
    /// Build group index mapping from Face.dat bytes like the Python script does.
    /// </summary>
    /// <param name="fileBytes">The raw bytes of Face.dat.</param>
    /// <returns>Dictionary where key is group name and value maps filename to index.</returns>
    public static Dictionary<string, Dictionary<string, int>> BuildGroupIndex(byte[] fileBytes)
    {
        var result = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        if (fileBytes == null || fileBytes.Length == 0)
            return result;

        // Split into lines by LF, trimming optional CR (same semantics as Python's strip on lines)
        int lineStart = 0;
        for (int i = 0; i <= fileBytes.Length; i++)
        {
            if (i == fileBytes.Length || fileBytes[i] == (byte)'\n')
            {
                int len = i - lineStart;
                if (len > 0)
                {
                    // Trim trailing CR if present
                    if (fileBytes[lineStart + len - 1] == (byte)'\r')
                        len--;

                    var line = new byte[len];
                    Buffer.BlockCopy(fileBytes, lineStart, line, 0, len);

                    ProcessLineForIndex(line, result);
                }

                lineStart = i + 1;
            }
        }

        return result;
    }

    /// <summary>
    /// Process a single decoded line and update the group dictionary if a valid entry is found.
    /// </summary>
    private static void ProcessLineForIndex(byte[] line, Dictionary<string, Dictionary<string, int>> groupDict)
    {
        // Find marker sequence
        int start = IndexOfSequence(line, EStrFileOrg, 0);
        if (start == -1)
            return;

        // Take bytes after marker plus 4 (matches Python behavior)
        int partStart = start + EStrFileOrg.Length + 4;
        if (partStart >= line.Length)
            return;

        int partLen = line.Length - partStart;
        var part = new byte[partLen];
        Buffer.BlockCopy(line, partStart, part, 0, partLen);

        var (key, idx) = FindKey(part, 0);
        if (key == null)
            return;

        var (eStrFileOrgValue, _) = GetPart(part, key.Value, idx);

        string dPart = XorDecodeToString(eStrFileOrgValue, key.Value);
        if (string.IsNullOrEmpty(dPart))
            return;

        // If the decoded part contains a colon, the Python script expects it to start with the prefix
        const string prefix = "UserDataCustomFace";
        string remainder;
        var colonParts = dPart.Split([':'], 2);
        if (colonParts.Length > 1)
        {
            if (!dPart.StartsWith(prefix, StringComparison.Ordinal))
                return; // same as Python: skip if prefix missing

            // Strip prefix and the following ':'
            int removeLen = prefix.Length + 1;
            if (dPart.Length <= removeLen)
                return;
            remainder = dPart.Substring(removeLen);
        }
        else
        {
            remainder = dPart;
        }

        // Split remainder by backslash to get group and filename
        var arr = remainder.Split(['\\'], StringSplitOptions.None);
        if (arr.Length < 2)
            return;

        string group = arr[0];
        string filename = arr[1];

        if (!groupDict.TryGetValue(group, out var files))
        {
            files = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            groupDict[group] = files;
        }

        if (!files.ContainsKey(filename))
        {
            files[filename] = files.Count;
        }
    }

    /// <summary>
    /// Locate subsequence in data starting at fromIndex, return -1 if not found
    /// </summary>
    private static int IndexOfSequence(byte[] data, byte[] seq, int fromIndex)
    {
        if (seq.Length == 0)
            return fromIndex <= data.Length ? fromIndex : -1;
        for (int i = fromIndex; i <= data.Length - seq.Length; i++)
        {
            bool ok = true;
            for (int j = 0; j < seq.Length; j++)
            {
                if (data[i + j] != seq[j])
                {
                    ok = false;
                    break;
                }
            }
            if (ok)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Equivalent to Python find_key: find a byte that repeats at offsets +2 and +4
    /// </summary>
    private static (byte? key, int seek) FindKey(byte[] data, int startIdx)
    {
        for (int i = startIdx; i + 4 < data.Length; i++)
        {
            byte b = data[i];
            if (b == data[i + 2] && b == data[i + 4])
                return (b, i);
        }
        return (null, 0);
    }

    /// <summary>
    /// Equivalent to Python get_part: extract the encrypted part starting at startIdx-1 up to end
    /// </summary>
    private static (byte[] part, int end) GetPart(byte[] data, byte key, int startIdx)
    {
        int end = 0;
        for (int i = startIdx; i < data.Length; i += 2)
        {
            if (data[i] != key)
            {
                end = i - 1;
                break;
            }
        }
        if (end == 0)
            end = data.Length - 1;

        int start = startIdx - 1;
        int length = end - start; // Python slice end is exclusive => length = end - (startIdx-1)
        if (length <= 0)
            return (Array.Empty<byte>(), end);

        var part = new byte[length];
        Buffer.BlockCopy(data, start, part, 0, length);
        return (part, end);
    }

    /// <summary>
    /// XOR-decode bytes and build a string, ignoring zero bytes (matches Python behavior)
    /// </summary>
    private static string XorDecodeToString(byte[] data, byte key)
    {
        if (data == null || data.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var b in data)
        {
            byte v = (byte)(b ^ key);
            if (v != 0)
                sb.Append((char)v);
        }
        return sb.ToString();
    }
}
