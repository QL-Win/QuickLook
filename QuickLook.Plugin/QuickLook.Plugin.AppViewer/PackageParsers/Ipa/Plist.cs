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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace QuickLook.Plugin.AppViewer.PackageParsers.Ipa;

/// <summary>
/// https://github.com/animetrics/PlistCS
/// </summary>
public static class Plist
{
    private static readonly List<int> offsetTable = [];
    private static List<byte> objectTable = [];
    private static int refCount;
    private static int objRefSize;
    private static int offsetByteSize;
    private static long offsetTableOffset;

    public static object ReadPlist(string path)
    {
        using var f = new FileStream(path, FileMode.Open, FileAccess.Read);
        return ReadPlist(f, PlistType.Auto);
    }

    public static object ReadPlistSource(string source)
    {
        return ReadPlist(Encoding.UTF8.GetBytes(source));
    }

    public static object ReadPlist(byte[] data)
    {
        return ReadPlist(new MemoryStream(data), PlistType.Auto);
    }

    public static PlistType GetPlistType(Stream stream)
    {
        byte[] magicHeader = new byte[8];
        stream.Read(magicHeader, 0, 8);

        if (BitConverter.ToInt64(magicHeader, 0) == 3472403351741427810L)
        {
            return PlistType.Binary;
        }
        else
        {
            return PlistType.Xml;
        }
    }

    public static object ReadPlist(Stream stream, PlistType type)
    {
        if (type == PlistType.Auto)
        {
            type = GetPlistType(stream);
            stream.Seek(0, SeekOrigin.Begin);
        }

        if (type == PlistType.Binary)
        {
            using var reader = new BinaryReader(stream);
            byte[] data = reader.ReadBytes((int)reader.BaseStream.Length);
            return ReadBinary(data);
        }
        else
        {
            XmlDocument xml = new()
            {
                XmlResolver = null,
            };
            xml.Load(stream);
            return ReadXml(xml);
        }
    }

    public static void WriteXml(object value, string path)
    {
        using var writer = new StreamWriter(path);
        writer.Write(WriteXml(value));
    }

    public static void WriteXml(object value, Stream stream)
    {
        using var writer = new StreamWriter(stream);
        writer.Write(WriteXml(value));
    }

    public static string WriteXml(object value)
    {
        using var ms = new MemoryStream();
        XmlWriterSettings xmlWriterSettings = new()
        {
            Encoding = new UTF8Encoding(false),
            ConformanceLevel = ConformanceLevel.Document,
            Indent = true,
        };

        using var xmlWriter = XmlWriter.Create(ms, xmlWriterSettings);
        xmlWriter.WriteStartDocument();
        //xmlWriter.WriteComment("DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" " + "\"http://www.apple.com/DTDs/PropertyList-1.0.dtd\"");
        xmlWriter.WriteDocType("plist", "-//Apple Computer//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null);
        xmlWriter.WriteStartElement("plist");
        xmlWriter.WriteAttributeString("version", "1.0");
        Compose(value, xmlWriter);
        xmlWriter.WriteEndElement();
        xmlWriter.WriteEndDocument();
        xmlWriter.Flush();
        xmlWriter.Close();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    public static void WriteBinary(object value, string path)
    {
        using var writer = new BinaryWriter(new FileStream(path, FileMode.Create));
        writer.Write(WriteBinary(value));
    }

    public static void WriteBinary(object value, Stream stream)
    {
        using var writer = new BinaryWriter(stream);
        writer.Write(WriteBinary(value));
    }

    public static byte[] WriteBinary(object value)
    {
        offsetTable.Clear();
        objectTable.Clear();
        refCount = 0;
        objRefSize = 0;
        offsetByteSize = 0;
        offsetTableOffset = 0;

        //Do not count the root node, subtract by 1
        int totalRefs = CountObject(value) - 1;

        refCount = totalRefs;

        objRefSize = RegulateNullBytes(BitConverter.GetBytes(refCount)).Length;

        ComposeBinary(value);

        WriteBinaryString("bplist00", false);

        offsetTableOffset = objectTable.Count;

        offsetTable.Add(objectTable.Count - 8);

        offsetByteSize = RegulateNullBytes(BitConverter.GetBytes(offsetTable[offsetTable.Count - 1])).Length;

        List<byte> offsetBytes = [];

        offsetTable.Reverse();

        for (int i = 0; i < offsetTable.Count; i++)
        {
            offsetTable[i] = objectTable.Count - offsetTable[i];
            byte[] buffer = RegulateNullBytes(BitConverter.GetBytes(offsetTable[i]), offsetByteSize);
            Array.Reverse(buffer);
            offsetBytes.AddRange(buffer);
        }

        objectTable.AddRange(offsetBytes);

        objectTable.AddRange(new byte[6]);
        objectTable.Add(Convert.ToByte(offsetByteSize));
        objectTable.Add(Convert.ToByte(objRefSize));

        var a = BitConverter.GetBytes((long)totalRefs + 1);
        Array.Reverse(a);
        objectTable.AddRange(a);

        objectTable.AddRange(BitConverter.GetBytes((long)0));
        a = BitConverter.GetBytes(offsetTableOffset);
        Array.Reverse(a);
        objectTable.AddRange(a);

        return [.. objectTable];
    }

    private static object ReadXml(XmlDocument xml)
    {
        XmlNode rootNode = xml.DocumentElement.ChildNodes[0];
        return Parse(rootNode);
    }

    private static object ReadBinary(byte[] data)
    {
        offsetTable.Clear();
        objectTable.Clear();
        refCount = 0;
        objRefSize = 0;
        offsetByteSize = 0;
        offsetTableOffset = 0;

        List<byte> bList = [.. data];

        List<byte> trailer = bList.GetRange(bList.Count - 32, 32);

        ParseTrailer(trailer);

        objectTable = bList.GetRange(0, (int)offsetTableOffset);

        List<byte> offsetTableBytes = bList.GetRange((int)offsetTableOffset, bList.Count - (int)offsetTableOffset - 32);

        ParseOffsetTable(offsetTableBytes);

        return ParseBinary(0);
    }

    private static Dictionary<string, object> ParseDictionary(XmlNode node)
    {
        XmlNodeList children = node.ChildNodes;
        if (children.Count % 2 != 0)
        {
            throw new DataMisalignedException("Dictionary elements must have an even number of child nodes");
        }

        Dictionary<string, object> dict = [];

        for (int i = 0; i < children.Count; i += 2)
        {
            XmlNode keynode = children[i];
            XmlNode valnode = children[i + 1];

            if (keynode.Name != "key")
            {
                throw new ApplicationException("expected a key node");
            }

            object result = Parse(valnode);

            if (result != null)
            {
                dict.Add(keynode.InnerText, result);
            }
        }

        //dict.Add("$Proxy", node);

        return dict;
    }

    private static List<object> ParseArray(XmlNode node)
    {
        List<object> array = [];

        foreach (XmlNode child in node.ChildNodes)
        {
            object result = Parse(child);
            if (result != null)
            {
                array.Add(result);
            }
        }

        return array;
    }

    private static void ComposeArray(List<object> value, XmlWriter writer)
    {
        writer.WriteStartElement("array");
        foreach (object obj in value)
        {
            Compose(obj, writer);
        }
        writer.WriteEndElement();
    }

    private static object Parse(XmlNode node)
    {
        return node.Name switch
        {
            "dict" => ParseDictionary(node),
            "array" => ParseArray(node),
            "string" => node.InnerText,
            "integer" => Convert.ToInt32(node.InnerText, NumberFormatInfo.InvariantInfo), // int result;
                                                                                          // int.TryParse(node.InnerText, System.Globalization.NumberFormatInfo.InvariantInfo, out result);
            "real" => Convert.ToDouble(node.InnerText, NumberFormatInfo.InvariantInfo),
            "false" => false,
            "true" => true,
            "null" => null,
            "date" => XmlConvert.ToDateTime(node.InnerText, XmlDateTimeSerializationMode.Utc),
            "data" => Convert.FromBase64String(node.InnerText),
            _ => throw new ApplicationException(string.Format("Plist Node `{0}' is not supported", node.Name)),
        };
    }

    private static void Compose(object value, XmlWriter writer)
    {
        if (value == null || value is string)
        {
            writer.WriteElementString("string", value as string);
        }
        else if (value is int || value is long)
        {
            writer.WriteElementString("integer", ((int)value).ToString(NumberFormatInfo.InvariantInfo));
        }
        else if (value is Dictionary<string, object> ||
          value.GetType().ToString().StartsWith("System.Collections.Generic.Dictionary`2[System.String"))
        {
            // Convert to Dictionary<string, object>
            if (value is not Dictionary<string, object> dic)
            {
                dic = [];
                IDictionary idic = (IDictionary)value;
                foreach (var key in idic.Keys)
                {
                    dic.Add(key.ToString(), idic[key]);
                }
            }
            WriteDictionaryValues(dic, writer);
        }
        else if (value is List<object> list)
        {
            ComposeArray(list, writer);
        }
        else if (value is byte[] bytes)
        {
            writer.WriteElementString("data", Convert.ToBase64String(bytes));
        }
        else if (value is float || value is double)
        {
            writer.WriteElementString("real", ((double)value).ToString(NumberFormatInfo.InvariantInfo));
        }
        else if (value is DateTime time)
        {
            string theString = XmlConvert.ToString(time, XmlDateTimeSerializationMode.Utc);
            writer.WriteElementString("date", theString);//, "yyyy-MM-ddTHH:mm:ssZ"));
        }
        else if (value is bool)
        {
            writer.WriteElementString(value.ToString().ToLower(), "");
        }
        else
        {
            throw new Exception(string.Format("Value type '{0}' is unhandled", value.GetType().ToString()));
        }
    }

    private static void WriteDictionaryValues(Dictionary<string, object> dictionary, XmlWriter writer)
    {
        writer.WriteStartElement("dict");
        foreach (string key in dictionary.Keys)
        {
            object value = dictionary[key];
            writer.WriteElementString("key", key);
            Compose(value, writer);
        }
        writer.WriteEndElement();
    }

    private static int CountObject(object value)
    {
        int count = 0;
        switch (value.GetType().ToString())
        {
            case "System.Collections.Generic.Dictionary`2[System.String,System.Object]":
                Dictionary<string, object> dict = (Dictionary<string, object>)value;
                foreach (string key in dict.Keys)
                {
                    count += CountObject(dict[key]);
                }
                count += dict.Keys.Count;
                count++;
                break;

            case "System.Collections.Generic.List`1[System.Object]":
                List<object> list = (List<object>)value;
                foreach (object obj in list)
                {
                    count += CountObject(obj);
                }
                count++;
                break;

            default:
                count++;
                break;
        }
        return count;
    }

    private static byte[] WriteBinaryDictionary(Dictionary<string, object> dictionary)
    {
        List<byte> buffer = [];
        List<byte> header = [];
        List<int> refs = [];
        for (int i = dictionary.Count - 1; i >= 0; i--)
        {
            var o = new object[dictionary.Count];
            dictionary.Values.CopyTo(o, 0);
            ComposeBinary(o[i]);
            offsetTable.Add(objectTable.Count);
            refs.Add(refCount);
            refCount--;
        }
        for (int i = dictionary.Count - 1; i >= 0; i--)
        {
            var o = new string[dictionary.Count];
            dictionary.Keys.CopyTo(o, 0);
            ComposeBinary(o[i]);
            offsetTable.Add(objectTable.Count);
            refs.Add(refCount);
            refCount--;
        }

        if (dictionary.Count < 15)
        {
            header.Add(Convert.ToByte(0xD0 | Convert.ToByte(dictionary.Count)));
        }
        else
        {
            header.Add(0xD0 | 0xf);
            header.AddRange(WriteBinaryInteger(dictionary.Count, false));
        }

        foreach (int val in refs)
        {
            byte[] refBuffer = RegulateNullBytes(BitConverter.GetBytes(val), objRefSize);
            Array.Reverse(refBuffer);
            buffer.InsertRange(0, refBuffer);
        }

        buffer.InsertRange(0, header);

        objectTable.InsertRange(0, buffer);

        return [.. buffer];
    }

    private static byte[] ComposeBinaryArray(List<object> objects)
    {
        List<byte> buffer = [];
        List<byte> header = [];
        List<int> refs = [];

        for (int i = objects.Count - 1; i >= 0; i--)
        {
            ComposeBinary(objects[i]);
            offsetTable.Add(objectTable.Count);
            refs.Add(refCount);
            refCount--;
        }

        if (objects.Count < 15)
        {
            header.Add(Convert.ToByte(0xA0 | Convert.ToByte(objects.Count)));
        }
        else
        {
            header.Add(0xA0 | 0xf);
            header.AddRange(WriteBinaryInteger(objects.Count, false));
        }

        foreach (int val in refs)
        {
            byte[] refBuffer = RegulateNullBytes(BitConverter.GetBytes(val), objRefSize);
            Array.Reverse(refBuffer);
            buffer.InsertRange(0, refBuffer);
        }

        buffer.InsertRange(0, header);

        objectTable.InsertRange(0, buffer);

        return [.. buffer];
    }

    private static byte[] ComposeBinary(object obj)
    {
        byte[] value;
        switch (obj.GetType().ToString())
        {
            case "System.Collections.Generic.Dictionary`2[System.String,System.Object]":
                value = WriteBinaryDictionary((Dictionary<string, object>)obj);
                return value;

            case "System.Collections.Generic.List`1[System.Object]":
                value = ComposeBinaryArray((List<object>)obj);
                return value;

            case "System.Byte[]":
                value = WriteBinaryByteArray((byte[])obj);
                return value;

            case "System.Double":
                value = WriteBinaryDouble((double)obj);
                return value;

            case "System.Int32":
                value = WriteBinaryInteger((int)obj, true);
                return value;

            case "System.String":
                value = WriteBinaryString((string)obj, true);
                return value;

            case "System.DateTime":
                value = WriteBinaryDate((DateTime)obj);
                return value;

            case "System.Boolean":
                value = WriteBinaryBool((bool)obj);
                return value;

            default:
                return [];
        }
    }

    public static byte[] WriteBinaryDate(DateTime obj)
    {
        List<byte> buffer = [.. RegulateNullBytes(BitConverter.GetBytes(PlistDateConverter.ConvertToAppleTimeStamp(obj)), 8)];
        buffer.Reverse();
        buffer.Insert(0, 0x33);
        objectTable.InsertRange(0, buffer);
        return [.. buffer];
    }

    public static byte[] WriteBinaryBool(bool obj)
    {
        List<byte> buffer = [.. new byte[1] { obj ? (byte)9 : (byte)8 }];
        objectTable.InsertRange(0, buffer);
        return [.. buffer];
    }

    private static byte[] WriteBinaryInteger(int value, bool write)
    {
        List<byte> buffer = [.. BitConverter.GetBytes((long)value)];
        buffer = [.. RegulateNullBytes([.. buffer])];
        while (buffer.Count != Math.Pow(2, Math.Log(buffer.Count) / Math.Log(2)))
            buffer.Add(0);
        int header = 0x10 | (int)(Math.Log(buffer.Count) / Math.Log(2));

        buffer.Reverse();

        buffer.Insert(0, Convert.ToByte(header));

        if (write)
            objectTable.InsertRange(0, buffer);

        return [.. buffer];
    }

    private static byte[] WriteBinaryDouble(double value)
    {
        List<byte> buffer = [.. RegulateNullBytes(BitConverter.GetBytes(value), 4)];
        while (buffer.Count != Math.Pow(2, Math.Log(buffer.Count) / Math.Log(2)))
            buffer.Add(0);
        int header = 0x20 | (int)(Math.Log(buffer.Count) / Math.Log(2));

        buffer.Reverse();

        buffer.Insert(0, Convert.ToByte(header));

        objectTable.InsertRange(0, buffer);

        return [.. buffer];
    }

    private static byte[] WriteBinaryByteArray(byte[] value)
    {
        List<byte> buffer = [.. value];
        List<byte> header = [];
        if (value.Length < 15)
        {
            header.Add(Convert.ToByte(0x40 | Convert.ToByte(value.Length)));
        }
        else
        {
            header.Add(0x40 | 0xf);
            header.AddRange(WriteBinaryInteger(buffer.Count, false));
        }

        buffer.InsertRange(0, header);

        objectTable.InsertRange(0, buffer);

        return [.. buffer];
    }

    private static byte[] WriteBinaryString(string value, bool head)
    {
        List<byte> buffer = [];
        List<byte> header = [];
        foreach (char chr in value.ToCharArray())
            buffer.Add(Convert.ToByte(chr));

        if (head)
        {
            if (value.Length < 15)
            {
                header.Add(Convert.ToByte(0x50 | Convert.ToByte(value.Length)));
            }
            else
            {
                header.Add(0x50 | 0xf);
                header.AddRange(WriteBinaryInteger(buffer.Count, false));
            }
        }

        buffer.InsertRange(0, header);

        objectTable.InsertRange(0, buffer);

        return [.. buffer];
    }

    private static byte[] RegulateNullBytes(byte[] value)
    {
        return RegulateNullBytes(value, 1);
    }

    private static byte[] RegulateNullBytes(byte[] value, int minBytes)
    {
        Array.Reverse(value);
        List<byte> bytes = [.. value];
        for (int i = 0; i < bytes.Count; i++)
        {
            if (bytes[i] == 0 && bytes.Count > minBytes)
            {
                bytes.Remove(bytes[i]);
                i--;
            }
            else
                break;
        }

        if (bytes.Count < minBytes)
        {
            int dist = minBytes - bytes.Count;
            for (int i = 0; i < dist; i++)
                bytes.Insert(0, 0);
        }

        value = [.. bytes];
        Array.Reverse(value);
        return value;
    }

    private static void ParseTrailer(List<byte> trailer)
    {
        offsetByteSize = BitConverter.ToInt32(RegulateNullBytes([.. trailer.GetRange(6, 1)], 4), 0);
        objRefSize = BitConverter.ToInt32(RegulateNullBytes([.. trailer.GetRange(7, 1)], 4), 0);
        byte[] refCountBytes = [.. trailer.GetRange(12, 4)];
        Array.Reverse(refCountBytes);
        refCount = BitConverter.ToInt32(refCountBytes, 0);
        byte[] offsetTableOffsetBytes = [.. trailer.GetRange(24, 8)];
        Array.Reverse(offsetTableOffsetBytes);
        offsetTableOffset = BitConverter.ToInt64(offsetTableOffsetBytes, 0);
    }

    private static void ParseOffsetTable(List<byte> offsetTableBytes)
    {
        for (int i = 0; i < offsetTableBytes.Count; i += offsetByteSize)
        {
            byte[] buffer = [.. offsetTableBytes.GetRange(i, offsetByteSize)];
            Array.Reverse(buffer);
            offsetTable.Add(BitConverter.ToInt32(RegulateNullBytes(buffer, 4), 0));
        }
    }

    private static object ParseBinaryDictionary(int objRef)
    {
        Dictionary<string, object> buffer = [];
        List<int> refs = [];
        int refCount = GetCount(offsetTable[objRef], out _);
        int refStartPosition;
        if (refCount < 15)
            refStartPosition = offsetTable[objRef] + 1;
        else
            refStartPosition = offsetTable[objRef] + 2 + RegulateNullBytes(BitConverter.GetBytes(refCount), 1).Length;

        for (int i = refStartPosition; i < refStartPosition + refCount * 2 * objRefSize; i += objRefSize)
        {
            byte[] refBuffer = [.. objectTable.GetRange(i, objRefSize)];
            Array.Reverse(refBuffer);
            refs.Add(BitConverter.ToInt32(RegulateNullBytes(refBuffer, 4), 0));
        }

        for (int i = 0; i < refCount; i++)
        {
            buffer.Add((string)ParseBinary(refs[i]), ParseBinary(refs[i + refCount]));
        }

        return buffer;
    }

    private static object ParseBinaryArray(int objRef)
    {
        List<object> buffer = [];
        List<int> refs = [];
        int refCount = GetCount(offsetTable[objRef], out _);
        int refStartPosition;
        if (refCount < 15)
            refStartPosition = offsetTable[objRef] + 1;
        else
            // The following integer has a header aswell so we increase the refStartPosition by two to account for that.
            refStartPosition = offsetTable[objRef] + 2 + RegulateNullBytes(BitConverter.GetBytes(refCount), 1).Length;

        for (int i = refStartPosition; i < refStartPosition + refCount * objRefSize; i += objRefSize)
        {
            byte[] refBuffer = [.. objectTable.GetRange(i, objRefSize)];
            Array.Reverse(refBuffer);
            refs.Add(BitConverter.ToInt32(RegulateNullBytes(refBuffer, 4), 0));
        }

        for (int i = 0; i < refCount; i++)
        {
            buffer.Add(ParseBinary(refs[i]));
        }

        return buffer;
    }

    private static int GetCount(int bytePosition, out int newBytePosition)
    {
        byte headerByte = objectTable[bytePosition];
        byte headerByteTrail = Convert.ToByte(headerByte & 0xf);
        int count;
        if (headerByteTrail < 15)
        {
            count = headerByteTrail;
            newBytePosition = bytePosition + 1;
        }
        else
            count = (int)ParseBinaryInt(bytePosition + 1, out newBytePosition);
        return count;
    }

    private static object ParseBinary(int objRef)
    {
        byte header = objectTable[offsetTable[objRef]];
        switch (header & 0xF0)
        {
            case 0:
                {
                    // If the byte is
                    // 0 return null
                    // 9 return true
                    // 8 return false
                    return objectTable[offsetTable[objRef]] == 0 ? null : objectTable[offsetTable[objRef]] == 9;
                }
            case 0x10:
                {
                    return ParseBinaryInt(offsetTable[objRef]);
                }
            case 0x20:
                {
                    return ParseBinaryReal(offsetTable[objRef]);
                }
            case 0x30:
                {
                    return ParseBinaryDate(offsetTable[objRef]);
                }
            case 0x40:
                {
                    return ParseBinaryByteArray(offsetTable[objRef]);
                }
            case 0x50: // String ASCII
                {
                    return ParseBinaryAsciiString(offsetTable[objRef]);
                }
            case 0x60: // String Unicode
                {
                    return ParseBinaryUnicodeString(offsetTable[objRef]);
                }
            case 0xD0:
                {
                    return ParseBinaryDictionary(objRef);
                }
            case 0xA0:
                {
                    return ParseBinaryArray(objRef);
                }
        }
        throw new Exception("This type is not supported");
    }

    public static object ParseBinaryDate(int headerPosition)
    {
        byte[] buffer = [.. objectTable.GetRange(headerPosition + 1, 8)];
        Array.Reverse(buffer);
        double appleTime = BitConverter.ToDouble(buffer, 0);
        DateTime result = PlistDateConverter.ConvertFromAppleTimeStamp(appleTime);
        return result;
    }

    private static object ParseBinaryInt(int headerPosition)
    {
        return ParseBinaryInt(headerPosition, out _);
    }

    private static object ParseBinaryInt(int headerPosition, out int newHeaderPosition)
    {
        byte header = objectTable[headerPosition];
        int byteCount = (int)Math.Pow(2, header & 0xf);
        byte[] buffer = [.. objectTable.GetRange(headerPosition + 1, byteCount)];
        Array.Reverse(buffer);
        // Add one to account for the header byte
        newHeaderPosition = headerPosition + byteCount + 1;
        return BitConverter.ToInt32(RegulateNullBytes(buffer, 4), 0);
    }

    private static object ParseBinaryReal(int headerPosition)
    {
        byte header = objectTable[headerPosition];
        int byteCount = (int)Math.Pow(2, header & 0xf);
        byte[] buffer = [.. objectTable.GetRange(headerPosition + 1, byteCount)];
        Array.Reverse(buffer);

        return BitConverter.ToDouble(RegulateNullBytes(buffer, 8), 0);
    }

    private static object ParseBinaryAsciiString(int headerPosition)
    {
        int charCount = GetCount(headerPosition, out int charStartPosition);
        var buffer = objectTable.GetRange(charStartPosition, charCount);
        return buffer.Count > 0 ? Encoding.ASCII.GetString([.. buffer]) : string.Empty;
    }

    private static object ParseBinaryUnicodeString(int headerPosition)
    {
        int charCount = GetCount(headerPosition, out int charStartPosition);
        charCount *= 2;

        byte[] buffer = new byte[charCount];
        byte one, two;

        for (int i = 0; i < charCount; i += 2)
        {
            one = objectTable.GetRange(charStartPosition + i, 1)[0];
            two = objectTable.GetRange(charStartPosition + i + 1, 1)[0];

            if (BitConverter.IsLittleEndian)
            {
                buffer[i] = two;
                buffer[i + 1] = one;
            }
            else
            {
                buffer[i] = one;
                buffer[i + 1] = two;
            }
        }

        return Encoding.Unicode.GetString(buffer);
    }

    private static object ParseBinaryByteArray(int headerPosition)
    {
        int byteCount = GetCount(headerPosition, out int byteStartPosition);
        return objectTable.GetRange(byteStartPosition, byteCount).ToArray();
    }
}

public enum PlistType
{
    Auto,
    Binary,
    Xml,
}

public static class PlistDateConverter
{
    public const long timeDifference = 978307200L;

    public static long GetAppleTime(long unixTime)
    {
        return unixTime - timeDifference;
    }

    public static long GetUnixTime(long appleTime)
    {
        return appleTime + timeDifference;
    }

    public static DateTime ConvertFromAppleTimeStamp(double timestamp)
    {
        DateTime origin = new(2001, 1, 1, 0, 0, 0, 0);
        return origin.AddSeconds(timestamp);
    }

    public static double ConvertToAppleTimeStamp(DateTime date)
    {
        DateTime begin = new(2001, 1, 1, 0, 0, 0, 0);
        TimeSpan diff = date - begin;
        return Math.Floor(diff.TotalSeconds);
    }
}
