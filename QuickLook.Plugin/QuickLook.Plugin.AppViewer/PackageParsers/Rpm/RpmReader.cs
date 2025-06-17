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

using PureSharpCompress.Common;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Rpm;

public class RpmReader
{
    public string Arch { get; set; }

    public string Version { get; set; }

    public string Name { get; set; }

    public string Exec { get; set; }

    public string Icon { get; set; }

    public Bitmap Logo { get; set; }

    public string Type { get; set; }

    public string Terminal { get; set; }

    public string[] Env { get; set; }

    public RpmReader(Stream stream)
    {
        Open(stream);
    }

    public RpmReader(string path)
    {
        using FileStream fs = File.OpenRead(path);
        Open(fs);
    }

    private void Open(Stream stream)
    {
        using var br = new BinaryReader(stream);

        // Step 1: Read the lead (96 bytes)
        byte[] lead = br.ReadBytes(96);
        Debug.WriteLine($"[lead] 96 bytes read, magic: {BitConverter.ToString(lead, 0, 4)}");

        // Step 2: Read signature header
        RpmHeader sigHeader = ReadHeader(br);
        Debug.WriteLine($"[signature header] IndexCount: {sigHeader.IndexCount}, DataSize: {sigHeader.DataSize}");

        // Step 3: Read main header
        RpmHeader mainHeader = ReadHeader(br);
        Debug.WriteLine($"[main header] IndexCount: {mainHeader.IndexCount}, DataSize: {mainHeader.DataSize}");

        // Step 4: Remaining is the payload (cpio archive + compression)
        long payloadSize = stream.Length - stream.Position;
        Debug.WriteLine($"[payload] Size: {payloadSize} bytes at offset: {stream.Position}");

        // (Optional) Detect compression (e.g. gzip, xz, zstd)
        byte[] magic = br.ReadBytes(6);
        string type = magic[0] switch
        {
            0x1F when magic[1] == 0x8B => "gzip",
            0xFD when magic[1] == 0x37 => "xz",
            0x28 when magic[1] == 0xB5 => "zstd",
            _ => "unknown"
        };
        Debug.WriteLine($"Detected payload compression: {type}");
    }

    private static RpmHeader ReadHeader(BinaryReader br)
    {
        // ed ab ee db 03
        byte[] magic = br.ReadBytes(3);
        //if (Encoding.ASCII.GetString(magic) != "\x8e\xad\xe8")
        //throw new InvalidDataException("Invalid RPM header magic");

        byte version = br.ReadByte(); // Usually 1
        byte[] reserved = br.ReadBytes(4);
        int indexCount = ReadBigEndianInt32(br);
        int dataSize = ReadBigEndianInt32(br);
        return new RpmHeader { IndexCount = indexCount, DataSize = dataSize };
    }

    private static int ReadBigEndianInt32(BinaryReader br)
    {
        byte[] b = br.ReadBytes(4);
        if (b.Length < 4) throw new EndOfStreamException();
        return (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3];
    }

    private class RpmHeader
    {
        public int IndexCount { get; set; }

        public int DataSize { get; set; }
    }
}
