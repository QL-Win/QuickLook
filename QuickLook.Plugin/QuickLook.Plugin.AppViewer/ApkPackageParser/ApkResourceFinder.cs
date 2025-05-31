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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace QuickLook.Plugin.AppViewer.ApkPackageParser;

/// <summary>
/// https://github.com/hylander0/Iteedee.ApkReader
/// </summary>
public class ApkResourceFinder
{
    private const short RES_STRING_POOL_TYPE = 0x0001;
    private const short RES_TABLE_TYPE = 0x0002;
    private const short RES_TABLE_PACKAGE_TYPE = 0x0200;
    private const short RES_TABLE_TYPE_TYPE = 0x0201;
    private const short RES_TABLE_TYPE_SPEC_TYPE = 0x0202;

    private string[] valueStringPool = null;
    private string[] typeStringPool = null;
    private string[] keyStringPool = null;

    private int package_id = 0;
    private List<string> resIdList;

    //// Contains no data.
    //static byte TYPE_NULL = 0x00;
    //// The 'data' holds an attribute resource identifier.
    //static byte TYPE_ATTRIBUTE = 0x02;
    //// The 'data' holds a single-precision floating point number.
    //static byte TYPE_FLOAT = 0x04;
    //// The 'data' holds a complex number encoding a dimension value,
    //// such as "100in".
    //static byte TYPE_DIMENSION = 0x05;
    //// The 'data' holds a complex number encoding a fraction of a
    //// container.
    //static byte TYPE_FRACTION = 0x06;
    //// The 'data' is a raw integer value of the form n..n.
    //static byte TYPE_INT_DEC = 0x10;
    //// The 'data' is a raw integer value of the form 0xn..n.
    //static byte TYPE_INT_HEX = 0x11;
    //// The 'data' is either 0 or 1, for input "false" or "true" respectively.
    //static byte TYPE_INT_BOOLEAN = 0x12;
    //// The 'data' is a raw integer value of the form #aarrggbb.
    //static byte TYPE_INT_COLOR_ARGB8 = 0x1c;
    //// The 'data' is a raw integer value of the form #rrggbb.
    //static byte TYPE_INT_COLOR_RGB8 = 0x1d;
    //// The 'data' is a raw integer value of the form #argb.
    //static byte TYPE_INT_COLOR_ARGB4 = 0x1e;
    //// The 'data' is a raw integer value of the form #rgb.
    //static byte TYPE_INT_COLOR_RGB4 = 0x1f;

    // The 'data' holds a ResTable_ref, a reference to another resource
    // table entry.
    private const byte TYPE_REFERENCE = 0x01;

    // The 'data' holds an index into the containing resource table's
    // global value string pool.
    private const byte TYPE_STRING = 0x03;

    private Dictionary<string, List<string>> responseMap;

    private Dictionary<int, List<string>> entryMap = [];

    public Dictionary<string, List<string>> Initialize()
    {
        byte[] data = File.ReadAllBytes("resources.arsc");
        return ProcessResourceTable(data, []);
    }

    public Dictionary<string, List<string>> ProcessResourceTable(byte[] data, List<string> resIdList)
    {
        this.resIdList = resIdList;

        responseMap = [];
        long lastPosition;

        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);
        short type = br.ReadInt16();
        short headerSize = br.ReadInt16();
        int size = br.ReadInt32();
        int packageCount = br.ReadInt32();

        if (type != RES_TABLE_TYPE)
        {
            throw new Exception("No RES_TABLE_TYPE found!");
        }
        if (size != br.BaseStream.Length)
        {
            throw new Exception("The buffer size not matches to the resource table size.");
        }

        int realStringPoolCount = 0;
        int realPackageCount = 0;

        while (true)
        {
            long pos = br.BaseStream.Position;
            short t = br.ReadInt16();
            short hs = br.ReadInt16();
            int s = br.ReadInt32();

            if (t == RES_STRING_POOL_TYPE)
            {
                if (realStringPoolCount == 0)
                {
                    // Only the first string pool is processed.
                    Debug.WriteLine("Processing the string pool ...");

                    byte[] buffer = new byte[s];
                    lastPosition = br.BaseStream.Position;
                    br.BaseStream.Seek(pos, SeekOrigin.Begin);
                    buffer = br.ReadBytes(s);
                    //br.BaseStream.Seek(lastPosition, SeekOrigin.Begin);

                    valueStringPool = ProcessStringPool(buffer);
                }
                realStringPoolCount++;
            }
            else if (t == RES_TABLE_PACKAGE_TYPE)
            {
                // Process the package
                Debug.WriteLine("Processing package {0} ...", realPackageCount);

                byte[] buffer = new byte[s];
                lastPosition = br.BaseStream.Position;
                br.BaseStream.Seek(pos, SeekOrigin.Begin);
                buffer = br.ReadBytes(s);
                //br.BaseStream.Seek(lastPosition, SeekOrigin.Begin);
                ProcessPackage(buffer);

                realPackageCount++;
            }
            else
            {
                throw new InvalidOperationException("Unsupported Type");
            }
            br.BaseStream.Seek(pos + s, SeekOrigin.Begin);
            if (br.BaseStream.Position == br.BaseStream.Length)
                break;
        }

        if (realStringPoolCount != 1)
        {
            throw new Exception("More than 1 string pool found!");
        }
        if (realPackageCount != packageCount)
        {
            throw new Exception("Real package count not equals the declared count.");
        }

        return responseMap;
    }

    private void ProcessPackage(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);
        //HEADER
        short type = br.ReadInt16();
        short headerSize = br.ReadInt16();
        int size = br.ReadInt32();

        int id = br.ReadInt32();
        package_id = id;

        //PackageName
        char[] name = new char[256];
        for (int i = 0; i < 256; ++i)
        {
            name[i] = br.ReadChar();
        }
        int typeStrings = br.ReadInt32();
        int lastPublicType = br.ReadInt32();
        int keyStrings = br.ReadInt32();
        int lastPublicKey = br.ReadInt32();

        if (typeStrings != headerSize)
        {
            throw new Exception("TypeStrings must immediately follow the package structure header.");
        }

        Debug.WriteLine("Type strings:");
        long lastPosition = br.BaseStream.Position;
        br.BaseStream.Seek(typeStrings, SeekOrigin.Begin);
        byte[] bbTypeStrings = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
        br.BaseStream.Seek(lastPosition, SeekOrigin.Begin);

        typeStringPool = ProcessStringPool(bbTypeStrings);

        Debug.WriteLine("Key strings:");

        br.BaseStream.Seek(keyStrings, SeekOrigin.Begin);
        short key_type = br.ReadInt16();
        short key_headerSize = br.ReadInt16();
        int key_size = br.ReadInt32();

        lastPosition = br.BaseStream.Position;
        br.BaseStream.Seek(keyStrings, SeekOrigin.Begin);
        byte[] bbKeyStrings = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
        br.BaseStream.Seek(lastPosition, SeekOrigin.Begin);

        keyStringPool = ProcessStringPool(bbKeyStrings);

        // Iterate through all chunks
        //
        int typeSpecCount = 0;
        int typeCount = 0;

        br.BaseStream.Seek((keyStrings + key_size), SeekOrigin.Begin);

        while (true)
        {
            int pos = (int)br.BaseStream.Position;
            short t = br.ReadInt16();
            short hs = br.ReadInt16();
            int s = br.ReadInt32();

            if (t == RES_TABLE_TYPE_SPEC_TYPE)
            {
                // Process the string pool
                byte[] buffer = new byte[s];
                br.BaseStream.Seek(pos, SeekOrigin.Begin);
                buffer = br.ReadBytes(s);

                ProcessTypeSpec(buffer);

                typeSpecCount++;
            }
            else if (t == RES_TABLE_TYPE_TYPE)
            {
                // Process the package
                byte[] buffer = new byte[s];
                br.BaseStream.Seek(pos, SeekOrigin.Begin);
                buffer = br.ReadBytes(s);

                ProcessType(buffer);

                typeCount++;
            }

            br.BaseStream.Seek(pos + s, SeekOrigin.Begin);
            if (br.BaseStream.Position == br.BaseStream.Length)
                break;
        }

        return;
    }

    private void PutIntoMap(string resId, string value)
    {
        List<string> valueList = null;
        if (responseMap.ContainsKey(resId.ToUpper()))
            valueList = responseMap[resId.ToUpper()];
        valueList ??= [];
        valueList.Add(value);
        if (responseMap.ContainsKey(resId.ToUpper()))
            responseMap[resId.ToUpper()] = valueList;
        else
            responseMap.Add(resId.ToUpper(), valueList);
        return;
    }

    private void ProcessType(byte[] typeData)
    {
        using var ms = new MemoryStream(typeData);
        using var br = new BinaryReader(ms);
        short type = br.ReadInt16();
        short headerSize = br.ReadInt16();
        int size = br.ReadInt32();
        byte id = br.ReadByte();
        byte res0 = br.ReadByte();
        short res1 = br.ReadInt16();
        int entryCount = br.ReadInt32();
        int entriesStart = br.ReadInt32();

        Dictionary<string, int> refKeys = [];

        int config_size = br.ReadInt32();

        // Skip the config data
        br.BaseStream.Seek(headerSize, SeekOrigin.Begin);

        if (headerSize + entryCount * 4 != entriesStart)
        {
            throw new Exception("HeaderSize, entryCount and entriesStart are not valid.");
        }

        // Start to get entry indices
        int[] entryIndices = new int[entryCount];
        for (int i = 0; i < entryCount; ++i)
        {
            entryIndices[i] = br.ReadInt32();
        }

        // Get entries
        for (int i = 0; i < entryCount; ++i)
        {
            if (entryIndices[i] == -1)
                continue;

            int resource_id = (package_id << 24) | (id << 16) | i;

            long pos = br.BaseStream.Position;
            short entry_size = br.ReadInt16();
            short entry_flag = br.ReadInt16();
            int entry_key = br.ReadInt32();

            // Get the value (simple) or map (complex)
            int FLAG_COMPLEX = 0x0001;

            if ((entry_flag & FLAG_COMPLEX) == 0)
            {
                // Simple case
                short value_size = br.ReadInt16();
                byte value_res0 = br.ReadByte();
                byte value_dataType = br.ReadByte();
                int value_data = br.ReadInt32();

                string idStr = resource_id.ToString("X4");
                string keyStr = keyStringPool[entry_key];
                string data = null;

                Debug.WriteLine("Entry 0x" + idStr + ", key: " + keyStr + ", simple value type: ");

                List<string> entryArr = null;
                if (entryMap.ContainsKey(int.Parse(idStr, System.Globalization.NumberStyles.HexNumber)))
                    entryArr = entryMap[int.Parse(idStr, System.Globalization.NumberStyles.HexNumber)];

                entryArr ??= [];

                entryArr.Add(keyStr);
                if (entryMap.ContainsKey(int.Parse(idStr, System.Globalization.NumberStyles.HexNumber)))
                    entryMap[int.Parse(idStr, System.Globalization.NumberStyles.HexNumber)] = entryArr;
                else
                    entryMap.Add(int.Parse(idStr, System.Globalization.NumberStyles.HexNumber), entryArr);

                if (value_dataType == TYPE_STRING)
                {
                    data = valueStringPool[value_data];
                    Debug.WriteLine(", data: " + valueStringPool[value_data]);
                }
                else if (value_dataType == TYPE_REFERENCE)
                {
                    string hexIndex = value_data.ToString("X4");
                    refKeys.Add(idStr, value_data);
                }
                else
                {
                    data = value_data.ToString();
                    Debug.WriteLine(", data: " + value_data);
                }

                // if (inReqList("@" + idStr)) {
                PutIntoMap("@" + idStr, data);
            }
            else
            {
                int entry_parent = br.ReadInt32();
                int entry_count = br.ReadInt32();

                for (int j = 0; j < entry_count; ++j)
                {
                    int ref_name = br.ReadInt32();
                    short value_size = br.ReadInt16();
                    byte value_res0 = br.ReadByte();
                    byte value_dataType = br.ReadByte();
                    int value_data = br.ReadInt32();
                }

                Debug.WriteLine("Entry 0x"
                    + resource_id.ToString("X4") + ", key: "
                    + keyStringPool[entry_key]
                    + ", complex value, not printed.");
            }
        }
        HashSet<string> refKs = [.. refKeys.Keys];

        foreach (string refK in refKs)
        {
            List<string> values = null;
            if (responseMap.ContainsKey("@" + refKeys[refK].ToString("X4").ToUpper()))
                values = responseMap["@" + refKeys[refK].ToString("X4").ToUpper()];

            if (values != null)
                foreach (string value in values)
                {
                    PutIntoMap("@" + refK, value);
                }
        }
        return;
    }

    private string[] ProcessStringPool(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);
        short type = br.ReadInt16();
        short headerSize = br.ReadInt16();
        int size = br.ReadInt32();
        int stringCount = br.ReadInt32();
        int styleCount = br.ReadInt32();
        int flags = br.ReadInt32();
        int stringsStart = br.ReadInt32();
        int stylesStart = br.ReadInt32();

        bool isUTF_8 = (flags & 256) != 0;

        int[] offsets = new int[stringCount];
        for (int i = 0; i < stringCount; ++i)
        {
            offsets[i] = br.ReadInt32();
        }
        string[] strings = new string[stringCount];

        for (int i = 0; i < stringCount; i++)
        {
            int pos = stringsStart + offsets[i];
            br.BaseStream.Seek(pos, SeekOrigin.Begin);
            strings[i] = string.Empty;
            if (isUTF_8)
            {
                int u16len = br.ReadByte(); // u16len
                if ((u16len & 0x80) != 0)
                {
                    // larger than 128
                    u16len = ((u16len & 0x7F) << 8) + br.ReadByte();
                }

                int u8len = br.ReadByte(); // u8len
                if ((u8len & 0x80) != 0)
                {
                    // larger than 128
                    u8len = ((u8len & 0x7F) << 8) + br.ReadByte();
                }

                if (u8len > 0)
                    strings[i] = Encoding.UTF8.GetString(br.ReadBytes(u8len));
                else
                    strings[i] = string.Empty;
            }
            else // UTF_16
            {
                int u16len = br.ReadUInt16();
                if ((u16len & 0x8000) != 0)
                {
                    // larger than 32768
                    u16len = ((u16len & 0x7FFF) << 16) + br.ReadUInt16();
                }

                if (u16len > 0)
                {
                    strings[i] = Encoding.Unicode.GetString(br.ReadBytes(u16len * 2));
                }
            }
            Debug.WriteLine("Parsed value: {0}", strings[i]);
        }
        return strings;
    }

    private void ProcessTypeSpec(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);
        short type = br.ReadInt16();
        short headerSize = br.ReadInt16();
        int size = br.ReadInt32();
        byte id = br.ReadByte();
        byte res0 = br.ReadByte();
        short res1 = br.ReadInt16();
        int entryCount = br.ReadInt32();

        Debug.WriteLine("Processing type spec {0}", typeStringPool[id - 1]);

        int[] flags = new int[entryCount];
        for (int i = 0; i < entryCount; ++i)
        {
            flags[i] = br.ReadInt32();
        }

        return;
    }
}
