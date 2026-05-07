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

namespace QuickLook.Plugin.DumpViewer;

public sealed class DumpInfo
{
    public string FilePath { get; set; }

    public DateTime LastWriteTime { get; set; }

    public DateTime? TimeStamp { get; set; }

    public string ProcessPath { get; set; }

    public string Architecture { get; set; }

    public string ExceptionCode { get; set; }

    public string ExceptionInformation { get; set; }

    public bool HasHeapInformation { get; set; }

    public bool HasErrorInformation { get; set; }

    public string OSVersion { get; set; }

    public string ClrVersions { get; set; }

    public string ParseError { get; set; }

    public List<DumpModuleInfo> Modules { get; } = [];
}

public sealed class DumpModuleInfo
{
    public string Name { get; set; }

    public string Version { get; set; }

    public string Path { get; set; }

    public ulong BaseAddress { get; set; }

    public uint Size { get; set; }
}

public sealed class KeyValueItem
{
    public KeyValueItem(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; }

    public string Value { get; }
}
