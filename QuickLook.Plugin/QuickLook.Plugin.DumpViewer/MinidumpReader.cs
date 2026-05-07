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
using System.Linq;
using System.Text;

namespace QuickLook.Plugin.DumpViewer;

internal static class MinidumpReader
{
    private const uint MinidumpSignature = 0x504D444D; // "MDMP"
    private const uint VsFixedFileInfoSignature = 0xFEEF04BD;
    private const int HeaderSize = 32;
    private const int DirectoryEntrySize = 12;
    private const int ModuleEntrySize = 108;

    public static bool IsMinidump(string path)
    {
        try
        {
            if (!File.Exists(path))
                return false;

            using var stream = OpenRead(path);
            using var reader = new BinaryReader(stream);

            if (stream.Length < HeaderSize)
                return false;

            return reader.ReadUInt32() == MinidumpSignature;
        }
        catch
        {
            return false;
        }
    }

    public static DumpInfo Read(string path)
    {
        var info = new DumpInfo
        {
            FilePath = path,
            LastWriteTime = File.GetLastWriteTime(path),
        };

        try
        {
            using var stream = OpenRead(path);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);

            var header = ReadHeader(reader, stream.Length);
            info.TimeStamp = UnixTimeToLocalTime(header.TimeDateStamp);

            var directories = ReadDirectories(reader, stream.Length, header)
                .GroupBy(i => i.Type)
                .ToDictionary(i => i.Key, i => i.First());

            ReadSystemInfo(info, reader, stream.Length, directories);
            ReadExceptionInfo(info, reader, stream.Length, directories);
            ReadModules(info, reader, stream.Length, directories);
            ReadSupplementalStreamInfo(info, directories);
            ReadClrVersions(info);
        }
        catch (Exception ex)
        {
            info.ParseError = ex.Message;
        }

        return info;
    }

    private static FileStream OpenRead(string path)
    {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    }

    private static MinidumpHeader ReadHeader(BinaryReader reader, long fileLength)
    {
        if (fileLength < HeaderSize)
            throw new InvalidDataException("The file is too small to be a minidump.");

        reader.BaseStream.Position = 0;

        var header = new MinidumpHeader
        {
            Signature = reader.ReadUInt32(),
            Version = reader.ReadUInt32(),
            NumberOfStreams = reader.ReadUInt32(),
            StreamDirectoryRva = reader.ReadUInt32(),
            CheckSum = reader.ReadUInt32(),
            TimeDateStamp = reader.ReadUInt32(),
            Flags = reader.ReadUInt64(),
        };

        if (header.Signature != MinidumpSignature)
            throw new InvalidDataException("The file does not have a minidump signature.");

        var directoryBytes = (long)header.NumberOfStreams * DirectoryEntrySize;
        if (!CanRead(fileLength, header.StreamDirectoryRva, directoryBytes))
            throw new InvalidDataException("The minidump stream directory is outside the file.");

        return header;
    }

    private static IEnumerable<MinidumpDirectory> ReadDirectories(BinaryReader reader, long fileLength, MinidumpHeader header)
    {
        reader.BaseStream.Position = header.StreamDirectoryRva;

        for (var i = 0; i < header.NumberOfStreams; i++)
        {
            var directory = new MinidumpDirectory
            {
                Type = (MinidumpStreamType)reader.ReadUInt32(),
                DataSize = reader.ReadUInt32(),
                Rva = reader.ReadUInt32(),
            };

            if (directory.DataSize == 0 || CanRead(fileLength, directory.Rva, directory.DataSize))
                yield return directory;
        }
    }

    private static void ReadSystemInfo(
        DumpInfo info,
        BinaryReader reader,
        long fileLength,
        IReadOnlyDictionary<MinidumpStreamType, MinidumpDirectory> directories)
    {
        if (!directories.TryGetValue(MinidumpStreamType.SystemInfoStream, out var directory)
            || directory.DataSize < 32
            || !CanRead(fileLength, directory.Rva, 32))
        {
            return;
        }

        reader.BaseStream.Position = directory.Rva;

        var processorArchitecture = reader.ReadUInt16();
        reader.BaseStream.Position = directory.Rva + 8;

        var majorVersion = reader.ReadUInt32();
        var minorVersion = reader.ReadUInt32();
        var buildNumber = reader.ReadUInt32();

        info.Architecture = ToProcessorArchitectureName(processorArchitecture);
        info.OSVersion = $"{majorVersion}.{minorVersion}.{buildNumber}";
    }

    private static void ReadExceptionInfo(
        DumpInfo info,
        BinaryReader reader,
        long fileLength,
        IReadOnlyDictionary<MinidumpStreamType, MinidumpDirectory> directories)
    {
        if (!directories.TryGetValue(MinidumpStreamType.ExceptionStream, out var directory)
            || directory.DataSize < 40
            || !CanRead(fileLength, directory.Rva, 40))
        {
            return;
        }

        reader.BaseStream.Position = directory.Rva;

        var threadId = reader.ReadUInt32();
        reader.BaseStream.Position = directory.Rva + 8;

        var exceptionCode = reader.ReadUInt32();
        reader.BaseStream.Position = directory.Rva + 24;

        var exceptionAddress = reader.ReadUInt64();

        info.ExceptionCode = FormatExceptionCode(exceptionCode);
        info.ExceptionInformation = $"Thread {threadId}, 0x{exceptionAddress:X}";
        info.HasErrorInformation = true;
    }

    private static void ReadModules(
        DumpInfo info,
        BinaryReader reader,
        long fileLength,
        IReadOnlyDictionary<MinidumpStreamType, MinidumpDirectory> directories)
    {
        if (!directories.TryGetValue(MinidumpStreamType.ModuleListStream, out var directory)
            || directory.DataSize < sizeof(uint)
            || !CanRead(fileLength, directory.Rva, sizeof(uint)))
        {
            return;
        }

        reader.BaseStream.Position = directory.Rva;
        var numberOfModules = reader.ReadUInt32();
        var maxModulesByStreamSize = (directory.DataSize - sizeof(uint)) / ModuleEntrySize;
        numberOfModules = Math.Min(numberOfModules, maxModulesByStreamSize);

        for (var i = 0; i < numberOfModules; i++)
        {
            var moduleOffset = directory.Rva + sizeof(uint) + i * ModuleEntrySize;
            if (!CanRead(fileLength, moduleOffset, ModuleEntrySize))
                break;

            reader.BaseStream.Position = moduleOffset;

            var module = new DumpModuleInfo
            {
                BaseAddress = reader.ReadUInt64(),
                Size = reader.ReadUInt32(),
            };

            reader.BaseStream.Position = moduleOffset + 20;
            var moduleNameRva = reader.ReadUInt32();

            module.Path = ReadMinidumpString(reader, fileLength, moduleNameRva);
            module.Name = Path.GetFileName(module.Path);
            if (string.IsNullOrEmpty(module.Name))
                module.Name = module.Path;

            reader.BaseStream.Position = moduleOffset + 24;
            module.Version = ReadVersion(reader);

            info.Modules.Add(module);
        }

        info.ProcessPath = info.Modules.FirstOrDefault()?.Path;
    }

    private static void ReadSupplementalStreamInfo(
        DumpInfo info,
        IReadOnlyDictionary<MinidumpStreamType, MinidumpDirectory> directories)
    {
        info.HasHeapInformation =
            directories.ContainsKey(MinidumpStreamType.MemoryListStream)
            || directories.ContainsKey(MinidumpStreamType.Memory64ListStream)
            || directories.ContainsKey(MinidumpStreamType.MemoryInfoListStream)
            || directories.ContainsKey(MinidumpStreamType.SystemMemoryInfoStream);

        info.HasErrorInformation =
            info.HasErrorInformation
            || directories.ContainsKey(MinidumpStreamType.HandleOperationListStream)
            || directories.ContainsKey(MinidumpStreamType.ProcessVmCountersStream);
    }

    private static void ReadClrVersions(DumpInfo info)
    {
        var clrModuleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "clr.dll",
            "coreclr.dll",
            "mscorwks.dll",
        };

        var versions = info.Modules
            .Where(i => clrModuleNames.Contains(i.Name))
            .Select(i => i.Version)
            .Where(i => !string.IsNullOrEmpty(i))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        info.ClrVersions = versions.Count == 0 ? string.Empty : string.Join(", ", versions);
    }

    private static string ReadMinidumpString(BinaryReader reader, long fileLength, uint rva)
    {
        if (rva == 0 || !CanRead(fileLength, rva, sizeof(uint)))
            return string.Empty;

        reader.BaseStream.Position = rva;
        var byteLength = reader.ReadUInt32();
        if (byteLength == 0)
            return string.Empty;

        const uint maxStringBytes = 1024 * 1024;
        byteLength = Math.Min(byteLength, maxStringBytes);

        if (!CanRead(fileLength, rva + sizeof(uint), byteLength))
            return string.Empty;

        var bytes = reader.ReadBytes((int)byteLength);

        return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
    }

    private static string ReadVersion(BinaryReader reader)
    {
        var signature = reader.ReadUInt32();
        reader.ReadUInt32(); // StrucVersion

        var fileVersionMs = reader.ReadUInt32();
        var fileVersionLs = reader.ReadUInt32();

        if (signature != VsFixedFileInfoSignature || (fileVersionMs == 0 && fileVersionLs == 0))
            return string.Empty;

        return $"{HiWord(fileVersionMs)}.{LoWord(fileVersionMs)}.{HiWord(fileVersionLs)}.{LoWord(fileVersionLs)}";
    }

    private static string FormatExceptionCode(uint exceptionCode)
    {
        var exceptionName = exceptionCode switch
        {
            0x80000003 => "Breakpoint",
            0xC0000005 => "Access violation",
            0xC000001D => "Illegal instruction",
            0xC000008C => "Array bounds exceeded",
            0xC000008D => "Float denormal operand",
            0xC000008E => "Float divide by zero",
            0xC000008F => "Float inexact result",
            0xC0000090 => "Float invalid operation",
            0xC0000091 => "Float overflow",
            0xC0000092 => "Float stack check",
            0xC0000093 => "Float underflow",
            0xC0000094 => "Integer divide by zero",
            0xC0000095 => "Integer overflow",
            0xC0000096 => "Privileged instruction",
            0xC00000FD => "Stack overflow",
            0xE0434352 => ".NET exception",
            _ => string.Empty,
        };

        var code = $"0x{exceptionCode:X8}";
        return string.IsNullOrEmpty(exceptionName) ? code : $"{code} ({exceptionName})";
    }

    private static string ToProcessorArchitectureName(ushort architecture)
    {
        return architecture switch
        {
            0 => "x86",
            1 => "MIPS",
            2 => "Alpha",
            3 => "PowerPC",
            4 => "SHx",
            5 => "ARM",
            6 => "IA64",
            7 => "Alpha64",
            9 => "x64",
            12 => "ARM64",
            13 => "ARM32",
            14 => "x86 on ARM64",
            0xFFFF => "Unknown",
            _ => architecture.ToString(),
        };
    }

    private static DateTime? UnixTimeToLocalTime(uint timeDateStamp)
    {
        if (timeDateStamp == 0)
            return null;

        try
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(timeDateStamp)
                .ToLocalTime();
        }
        catch
        {
            return null;
        }
    }

    private static bool CanRead(long fileLength, long offset, long bytes)
    {
        return offset >= 0 && bytes >= 0 && offset <= fileLength && bytes <= fileLength - offset;
    }

    private static ushort HiWord(uint value)
    {
        return (ushort)(value >> 16);
    }

    private static ushort LoWord(uint value)
    {
        return (ushort)(value & 0xFFFF);
    }

    private sealed class MinidumpHeader
    {
        public uint Signature { get; set; }

        public uint Version { get; set; }

        public uint NumberOfStreams { get; set; }

        public uint StreamDirectoryRva { get; set; }

        public uint CheckSum { get; set; }

        public uint TimeDateStamp { get; set; }

        public ulong Flags { get; set; }
    }

    private sealed class MinidumpDirectory
    {
        public MinidumpStreamType Type { get; set; }

        public uint DataSize { get; set; }

        public uint Rva { get; set; }
    }

    private enum MinidumpStreamType : uint
    {
        ThreadListStream = 3,
        ModuleListStream = 4,
        MemoryListStream = 5,
        ExceptionStream = 6,
        SystemInfoStream = 7,
        ThreadExListStream = 8,
        Memory64ListStream = 9,
        CommentStreamA = 10,
        CommentStreamW = 11,
        HandleDataStream = 12,
        FunctionTableStream = 13,
        UnloadedModuleListStream = 14,
        MiscInfoStream = 15,
        MemoryInfoListStream = 16,
        ThreadInfoListStream = 17,
        HandleOperationListStream = 18,
        TokenStream = 19,
        JavaScriptDataStream = 20,
        SystemMemoryInfoStream = 21,
        ProcessVmCountersStream = 22,
        IptTraceStream = 23,
        ThreadNamesStream = 24,
    }
}
