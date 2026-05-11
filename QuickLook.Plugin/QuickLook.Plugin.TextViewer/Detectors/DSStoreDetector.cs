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

public sealed class DSStoreDetector : ITransferFormatDetector
{
    public string Name => "YAML";

    public string Extension => ".yaml";

    public string RealExtension => ".DS_Store";

    public DSStoreDetector()
    {
    }

    public bool Detect(string path, string text)
    {
        _ = text;
        if (string.IsNullOrEmpty(path)) return false;
        return Path.GetFileName(path).Equals(RealExtension, StringComparison.OrdinalIgnoreCase);
    }

    public string Transfer(string path)
    {
        if (!Detect(path, null)) return null;

        try
        {
            var data = File.ReadAllBytes(path);
            var a = new DSStoreAllocator(data);
            var files = a.TraverseFromRootNode();

            var sb = new StringBuilder();
            sb.AppendLine($"DS_Store: {Path.GetFileName(path)}");
            sb.AppendLine($"Entries: {files.Count}");
            sb.AppendLine();
            foreach (var f in files)
            {
                sb.AppendLine(f);
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            try
            {
                // fallback: try to discover UTF-16BE strings
                var fallback = ScanUtf16Be(File.ReadAllBytes(path));
                var sb = new StringBuilder();
                sb.AppendLine($"DS_Store parse failed: {ex.Message}");
                sb.AppendLine("Fallback UTF-16BE strings:");
                sb.AppendLine();
                foreach (var s in fallback)
                    sb.AppendLine(s);
                return sb.ToString();
            }
            catch (Exception ex2)
            {
                return $"Failed to parse DS_Store: {ex.Message}; {ex2.Message}";
            }
        }
    }

    private static List<string> ScanUtf16Be(byte[] data)
    {
        var results = new List<string>();
        var sb = new StringBuilder();
        for (int i = 0; i + 1 < data.Length; i += 2)
        {
            // treat as big-endian UTF-16 code unit
            ushort code = (ushort)((data[i] << 8) | data[i + 1]);
            if (code >= 0x20 && code <= 0x7e) // printable ASCII
            {
                sb.Append((char)code);
            }
            else
            {
                if (sb.Length >= 2)
                {
                    results.Add(sb.ToString());
                }
                sb.Clear();
            }
        }
        if (sb.Length >= 2)
            results.Add(sb.ToString());
        return results;
    }

    // Lightweight port of the relevant DS_Store allocator/block logic used to extract filenames.
    private class DSStoreAllocator
    {
        public byte[] Data { get; }
        public uint Pos { get; private set; }
        public DSStoreBlock Root { get; private set; }
        public List<uint> Offsets { get; } = [];
#pragma warning disable IDE0028 // Simplify collection initialization
        public Dictionary<string, uint> Toc { get; } = new(StringComparer.Ordinal);
#pragma warning restore IDE0028 // Simplify collection initialization
        public Dictionary<uint, List<uint>> FreeList { get; } = [];

        public DSStoreAllocator(byte[] data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Pos = 0;
            var (offset, size) = ReadHeader();
            Root = NewBlock(offset, size);
            ReadOffsets();
            ReadToc();
            ReadFreeList();
        }

        private (uint offset, uint size) ReadHeader()
        {
            if (Data.Length < 32) throw new InvalidDataException("Header not long enough");

            uint magic1 = ReadUint32BE(Data, (int)Pos);
            if (magic1 != 1) throw new InvalidDataException("Wrong magic bytes");
            Pos += 4;

            uint magic = ReadUint32BE(Data, (int)Pos);
            if (magic != 0x42756431u) throw new InvalidDataException("Wrong magic bytes");
            Pos += 4;

            uint offset = ReadUint32BE(Data, (int)Pos); Pos += 4;
            uint size = ReadUint32BE(Data, (int)Pos); Pos += 4;
            uint offset2 = ReadUint32BE(Data, (int)Pos);
            if (offset != offset2) throw new InvalidDataException("Offsets do not match");
            Pos += 4;

            return (offset, size);
        }

        private DSStoreBlock NewBlock(uint pos, uint size)
        {
            if (Data.Length < pos + 4 + size) throw new InvalidDataException("Not enough Data for block");
            var buf = new byte[size];
            Buffer.BlockCopy(Data, (int)pos + 4, buf, 0, (int)size);
            return new DSStoreBlock(this, pos, size, buf);
        }

        private void ReadOffsets()
        {
            uint count = Root.ReadUint32();
            Root.Skip(4);
            for (int offcount = (int)count; offcount > 0; offcount -= 256)
            {
                for (int i = 0; i < 256; i++)
                {
                    uint val = Root.ReadUint32();
                    if (val == 0) continue;
                    Offsets.Add(val);
                }
            }
        }

        private void ReadToc()
        {
            uint toccount = Root.ReadUint32();
            for (uint i = toccount; i > 0; i--)
            {
                byte tlen = Root.ReadByte();
                var name = Root.ReadBuf(tlen);
                uint value = Root.ReadUint32();
                Toc[Encoding.ASCII.GetString(name)] = value;
            }
        }

        private void ReadFreeList()
        {
            for (int i = 0; i < 32; i++)
            {
                uint blkcount = Root.ReadUint32();
                if (blkcount == 0) continue;
                var list = new List<uint>();
                for (int k = 0; k < (int)blkcount; k++)
                {
                    uint val = Root.ReadUint32();
                    if (val == 0) continue;
                    list.Add(val);
                }
                FreeList[(uint)i] = list;
            }
        }

        public List<string> TraverseFromRootNode()
        {
            if (!Toc.TryGetValue("DSDB", out var tocVal))
                return [];
            var rootBlk = GetBlock(tocVal);
            uint rootNode = rootBlk.ReadUint32();
            rootBlk.Skip(4 * 4);
            return Traverse(rootNode);
        }

        public DSStoreBlock GetBlock(uint bid)
        {
            if (Offsets.Count <= (int)bid) throw new IndexOutOfRangeException("Cannot find key in Offset-Table");
            uint addr = Offsets[(int)bid];
            int offset = (int)(addr & ~0x1Fu);
            int size = 1 << (int)(addr & 0x1Fu);
            return NewBlock((uint)offset, (uint)size);
        }

        private List<string> Traverse(uint bid)
        {
            var filenames = new List<string>();
            var node = GetBlock(bid);
            uint nextPtr = node.ReadUint32();
            uint count = node.ReadUint32();
            if (nextPtr > 0)
            {
                for (int i = 0; i < (int)count; i++)
                {
                    uint next = node.ReadUint32();
                    var files = Traverse(next);
                    filenames.AddRange(files);
                    var f = node.ReadFileName();
                    filenames.Add(f);
                }
                var rfiles = Traverse(nextPtr);
                filenames.AddRange(rfiles);
            }
            else
            {
                for (int i = 0; i < (int)count; i++)
                {
                    var f = node.ReadFileName();
                    filenames.Add(f);
                }
            }
            return filenames;
        }

        private static uint ReadUint32BE(byte[] arr, int offset)
        {
            return ((uint)arr[offset] << 24) | ((uint)arr[offset + 1] << 16) | ((uint)arr[offset + 2] << 8) | ((uint)arr[offset + 3]);
        }
    }

    private class DSStoreBlock(DSStoreDetector.DSStoreAllocator alloc, uint offset, uint size, byte[] data)
    {
        private readonly DSStoreAllocator _alloc = alloc;
        private readonly byte[] _data = data;

        public uint Offset { get; } = offset;

        public uint Size { get; } = size;

        public uint Pos { get; private set; } = 0;

        public uint ReadUint32()
        {
            if (Size - Pos < 4) throw new EndOfStreamException("Not enough bytes to read");
            uint v = ((uint)_data[Pos] << 24) | ((uint)_data[Pos + 1] << 16) | ((uint)_data[Pos + 2] << 8) | ((uint)_data[Pos + 3]);
            Pos += 4;
            return v;
        }

        public byte ReadByte()
        {
            if (Size - Pos < 1) throw new EndOfStreamException("Not enough bytes to read");
            byte v = _data[Pos];
            Pos += 1;
            return v;
        }

        public byte[] ReadBuf(int length)
        {
            if ((int)Size - (int)Pos < length) throw new EndOfStreamException("Not enough bytes to read");
            var buf = new byte[length];
            Buffer.BlockCopy(_data, (int)Pos, buf, 0, length);
            Pos += (uint)length;
            return buf;
        }

        public string ReadFileName()
        {
            uint length = ReadUint32();
            var buf = ReadBuf((int)(2 * length));
            // skip 4 bytes (sid)
            Skip(4);
            var stype = ReadBuf(4);
            string t = Encoding.ASCII.GetString(stype);
            int bytesToSkip = -1;
            switch (t)
            {
                case "bool": bytesToSkip = 1; break;
                case "type":
                case "long":
                case "shor": bytesToSkip = 4; break;
                case "comp":
                case "dutc": bytesToSkip = 8; break;
                case "blob":
                    {
                        uint blen = ReadUint32();
                        bytesToSkip = (int)blen;
                        break;
                    }
                case "ustr":
                    {
                        uint blen = ReadUint32();
                        bytesToSkip = (int)(2 * blen);
                        break;
                    }
                default: break;
            }
            if (bytesToSkip <= 0) throw new InvalidDataException("Unknown file format");
            Skip((uint)bytesToSkip);
            // decode UTF-16BE buffer
            string name = Encoding.BigEndianUnicode.GetString(buf);
            return name;
        }

        public void Skip(uint i)
        {
            Pos += i;
        }
    }
}
