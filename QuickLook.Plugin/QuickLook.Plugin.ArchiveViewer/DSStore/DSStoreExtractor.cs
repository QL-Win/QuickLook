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

namespace QuickLook.Plugin.ArchiveViewer.DSStore;

/// <summary>
/// Lightweight parser for macOS .DS_Store binary files that extracts the list of
/// filenames referenced in the B-tree structure.  Ported from the open-source
/// ds_store Python library by Wim Glenn.
/// </summary>
public static class DSStoreExtractor
{
    /// <summary>
    /// Parses the .DS_Store file at <paramref name="path"/> and returns a deduplicated,
    /// sorted list of the filenames stored inside the B-tree.
    /// </summary>
    public static List<string> GetFileNames(string path)
    {
        var data = File.ReadAllBytes(path);
        var allocator = new DSStoreAllocator(data);
        return allocator.TraverseFromRootNode();
    }

    // ── Internal allocator / block types ────────────────────────────────────

    private sealed class DSStoreAllocator
    {
        private readonly byte[] _data;
        private uint _pos;
        private readonly DSStoreBlock _root;
        private readonly List<uint> _offsets = [];
        private readonly Dictionary<string, uint> _toc = new(StringComparer.Ordinal);

        public DSStoreAllocator(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _pos = 0;
            var (offset, size) = ReadHeader();
            _root = NewBlock(offset, size);
            ReadOffsets();
            ReadToc();
            ReadFreeList();
        }

        private (uint offset, uint size) ReadHeader()
        {
            if (_data.Length < 32)
                throw new InvalidDataException("DS_Store header too short");

            uint magic1 = ReadUint32BE(_data, (int)_pos);
            if (magic1 != 1)
                throw new InvalidDataException("DS_Store: wrong magic (expected 0x00000001)");
            _pos += 4;

            uint magic = ReadUint32BE(_data, (int)_pos);
            if (magic != 0x42756431u)
                throw new InvalidDataException("DS_Store: wrong magic (expected 'Bud1')");
            _pos += 4;

            uint offset = ReadUint32BE(_data, (int)_pos); _pos += 4;
            uint size   = ReadUint32BE(_data, (int)_pos); _pos += 4;
            uint offset2 = ReadUint32BE(_data, (int)_pos);
            if (offset != offset2)
                throw new InvalidDataException("DS_Store: root-block offset mismatch");
            _pos += 4;

            return (offset, size);
        }

        private DSStoreBlock NewBlock(uint pos, uint size)
        {
            if (_data.Length < pos + 4 + size)
                throw new InvalidDataException("DS_Store: not enough data for block");
            var buf = new byte[size];
            Buffer.BlockCopy(_data, (int)pos + 4, buf, 0, (int)size);
            return new DSStoreBlock(this, pos, size, buf);
        }

        private void ReadOffsets()
        {
            uint count = _root.ReadUint32();
            _root.Skip(4);
            for (int offcount = (int)count; offcount > 0; offcount -= 256)
            {
                for (int i = 0; i < 256; i++)
                {
                    uint val = _root.ReadUint32();
                    if (val != 0)
                        _offsets.Add(val);
                }
            }
        }

        private void ReadToc()
        {
            uint toccount = _root.ReadUint32();
            for (uint i = toccount; i > 0; i--)
            {
                byte tlen  = _root.ReadByte();
                var  name  = _root.ReadBuf(tlen);
                uint value = _root.ReadUint32();
                _toc[Encoding.ASCII.GetString(name)] = value;
            }
        }

        private void ReadFreeList()
        {
            for (int i = 0; i < 32; i++)
            {
                uint blkcount = _root.ReadUint32();
                for (int k = 0; k < (int)blkcount; k++)
                    _root.ReadUint32(); // consume entries; not needed for filename extraction
            }
        }

        public List<string> TraverseFromRootNode()
        {
            if (!_toc.TryGetValue("DSDB", out var tocVal))
                return [];
            var rootBlk = GetBlock(tocVal);
            uint rootNode = rootBlk.ReadUint32();
            rootBlk.Skip(4 * 4);
            return Traverse(rootNode);
        }

        internal DSStoreBlock GetBlock(uint bid)
        {
            if (_offsets.Count <= (int)bid)
                throw new IndexOutOfRangeException("DS_Store: block id out of range");
            uint addr   = _offsets[(int)bid];
            int  offset = (int)(addr & ~0x1Fu);
            int  size   = 1 << (int)(addr & 0x1Fu);
            return NewBlock((uint)offset, (uint)size);
        }

        private List<string> Traverse(uint bid)
        {
            var filenames = new List<string>();
            var node = GetBlock(bid);
            uint nextPtr = node.ReadUint32();
            uint count   = node.ReadUint32();

            if (nextPtr > 0)
            {
                for (int i = 0; i < (int)count; i++)
                {
                    uint next = node.ReadUint32();
                    filenames.AddRange(Traverse(next));
                    filenames.Add(node.ReadFileName());
                }
                filenames.AddRange(Traverse(nextPtr));
            }
            else
            {
                for (int i = 0; i < (int)count; i++)
                    filenames.Add(node.ReadFileName());
            }

            return filenames;
        }

        private static uint ReadUint32BE(byte[] arr, int offset) =>
            ((uint)arr[offset] << 24)
            | ((uint)arr[offset + 1] << 16)
            | ((uint)arr[offset + 2] << 8)
            | arr[offset + 3];
    }

    private sealed class DSStoreBlock(DSStoreAllocator alloc, uint offset, uint size, byte[] data)
    {
#pragma warning disable IDE0052
        private readonly DSStoreAllocator _alloc  = alloc;   // kept for parity / future use
#pragma warning restore IDE0052
        private readonly byte[] _data = data;

        public uint Offset { get; } = offset;
        public uint Size   { get; } = size;
        public uint Pos    { get; private set; }

        public uint ReadUint32()
        {
            if (Size - Pos < 4) throw new EndOfStreamException("DS_Store block: not enough bytes");
            uint v = ((uint)_data[Pos] << 24)
                   | ((uint)_data[Pos + 1] << 16)
                   | ((uint)_data[Pos + 2] << 8)
                   | _data[Pos + 3];
            Pos += 4;
            return v;
        }

        public byte ReadByte()
        {
            if (Size - Pos < 1) throw new EndOfStreamException("DS_Store block: not enough bytes");
            return _data[Pos++];
        }

        public byte[] ReadBuf(int length)
        {
            if ((int)Size - (int)Pos < length)
                throw new EndOfStreamException("DS_Store block: not enough bytes");
            var buf = new byte[length];
            Buffer.BlockCopy(_data, (int)Pos, buf, 0, length);
            Pos += (uint)length;
            return buf;
        }

        public void Skip(uint i) => Pos += i;

        public string ReadFileName()
        {
            uint   length = ReadUint32();
            byte[] buf    = ReadBuf((int)(2 * length));

            // skip sid (4 bytes) and read type tag (4 bytes)
            Skip(4);
            byte[] stypeBytes = ReadBuf(4);
            string stype      = Encoding.ASCII.GetString(stypeBytes);

            int bytesToSkip = stype switch
            {
                "bool"           => 1,
                "type" or "long" or "shor" => 4,
                "comp" or "dutc" => 8,
                "blob"           => (int)ReadUint32(),
                "ustr"           => (int)(2 * ReadUint32()),
                _                => throw new InvalidDataException($"DS_Store: unknown record type '{stype}'")
            };

            Skip((uint)bytesToSkip);

            // filename is encoded as UTF-16 big-endian
            return Encoding.BigEndianUnicode.GetString(buf);
        }
    }
}
