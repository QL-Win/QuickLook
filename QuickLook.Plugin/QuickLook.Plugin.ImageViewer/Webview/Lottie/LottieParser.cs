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
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace QuickLook.Plugin.ImageViewer.Webview.Lottie;

internal static class LottieParser
{
    [ThreadStatic] private static Stack<List<string>> splitArrayPool = null!;
    [ThreadStatic] private static StringBuilder stringBuilder = null!;
    [ThreadStatic] private static Dictionary<Type, Dictionary<string, FieldInfo>> fieldInfoCache = null!;
    [ThreadStatic] private static Dictionary<Type, Dictionary<string, PropertyInfo>> propertyInfoCache = null!;

    public static T Parse<T>(string json)
    {
        // Initialize, if needed, the ThreadStatic variables
        propertyInfoCache ??= [];
        fieldInfoCache ??= [];
        stringBuilder ??= new StringBuilder();
        splitArrayPool ??= new Stack<List<string>>();

        //Remove all whitespace not within strings to make parsing simpler
        stringBuilder.Length = 0;
        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '"')
            {
                i = AppendUntilStringEnd(true, i, json);
                continue;
            }
            if (char.IsWhiteSpace(c))
                continue;

            stringBuilder.Append(c);
        }

        //Parse the thing!
        return (T)ParseValue(typeof(T), stringBuilder.ToString());
    }

    static int AppendUntilStringEnd(bool appendEscapeCharacter, int startIdx, string json)
    {
        stringBuilder.Append(json[startIdx]);
        for (int i = startIdx + 1; i < json.Length; i++)
        {
            if (json[i] == '\\')
            {
                if (appendEscapeCharacter)
                    stringBuilder.Append(json[i]);
                stringBuilder.Append(json[i + 1]);
                i++;//Skip next character as it is escaped
            }
            else if (json[i] == '"')
            {
                stringBuilder.Append(json[i]);
                return i;
            }
            else
                stringBuilder.Append(json[i]);
        }
        return json.Length - 1;
    }

    //Splits { <value>:<value>, <value>:<value> } and [ <value>, <value> ] into a list of <value> strings
    static List<string> Split(string json)
    {
        List<string> splitArray = splitArrayPool.Count > 0 ? splitArrayPool.Pop() : [];
        splitArray.Clear();
        if (json.Length == 2)
            return splitArray;
        int parseDepth = 0;
        stringBuilder.Length = 0;
        for (int i = 1; i < json.Length - 1; i++)
        {
            switch (json[i])
            {
                case '[':
                case '{':
                    parseDepth++;
                    break;

                case ']':
                case '}':
                    parseDepth--;
                    break;

                case '"':
                    i = AppendUntilStringEnd(true, i, json);
                    continue;
                case ',':
                case ':':
                    if (parseDepth == 0)
                    {
                        splitArray.Add(stringBuilder.ToString());
                        stringBuilder.Length = 0;
                        continue;
                    }
                    break;
            }

            stringBuilder.Append(json[i]);
        }

        splitArray.Add(stringBuilder.ToString());

        return splitArray;
    }

    internal static object ParseValue(Type type, string json)
    {
        if (type == typeof(string))
        {
            if (json.Length <= 2)
                return string.Empty;
            StringBuilder parseStringBuilder = new(json.Length);
            for (int i = 1; i < json.Length - 1; ++i)
            {
                if (json[i] == '\\' && i + 1 < json.Length - 1)
                {
                    int j = "\"\\nrtbf/".IndexOf(json[i + 1]);
                    if (j >= 0)
                    {
                        parseStringBuilder.Append("\"\\\n\r\t\b\f/"[j]);
                        ++i;
                        continue;
                    }
                    if (json[i + 1] == 'u' && i + 5 < json.Length - 1)
                    {
                        if (uint.TryParse(json.Substring(i + 2, 4), System.Globalization.NumberStyles.AllowHexSpecifier, null, out uint c))
                        {
                            parseStringBuilder.Append((char)c);
                            i += 5;
                            continue;
                        }
                    }
                }
                parseStringBuilder.Append(json[i]);
            }
            return parseStringBuilder.ToString();
        }
        if (type.IsPrimitive)
        {
            var result = Convert.ChangeType(json, type, System.Globalization.CultureInfo.InvariantCulture);
            return result;
        }
        if (type == typeof(decimal))
        {
            decimal.TryParse(json, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out decimal result);
            return result;
        }
        if (type == typeof(DateTime))
        {
            DateTime.TryParse(json.Replace("\"", ""), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime result);
            return result;
        }
        if (json == "null")
        {
            return null!;
        }
        if (type.IsEnum)
        {
            if (json[0] == '"')
                json = json.Substring(1, json.Length - 2);
            try
            {
                return Enum.Parse(type, json, false);
            }
            catch
            {
                return 0;
            }
        }
        if (type.IsArray)
        {
            Type arrayType = type.GetElementType();
            if (json[0] != '[' || json[json.Length - 1] != ']')
                return null!;

            List<string> elems = Split(json);
            Array newArray = Array.CreateInstance(arrayType, elems.Count);
            for (int i = 0; i < elems.Count; i++)
                newArray.SetValue(ParseValue(arrayType, elems[i]), i);
            splitArrayPool.Push(elems);
            return newArray;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            Type listType = type.GetGenericArguments()[0];
            if (json[0] != '[' || json[json.Length - 1] != ']')
                return null!;

            List<string> elems = Split(json);
            var list = (IList)type.GetConstructor([typeof(int)]).Invoke([elems.Count]);
            for (int i = 0; i < elems.Count; i++)
                list.Add(ParseValue(listType, elems[i]));
            splitArrayPool.Push(elems);
            return list;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            Type keyType, valueType;
            {
                Type[] args = type.GetGenericArguments();
                keyType = args[0];
                valueType = args[1];
            }

            //Refuse to parse dictionary keys that aren't of type string
            if (keyType != typeof(string))
                return null!;
            //Must be a valid dictionary element
            if (json[0] != '{' || json[json.Length - 1] != '}')
                return null!;
            //The list is split into key/value pairs only, this means the split must be divisible by 2 to be valid JSON
            List<string> elems = Split(json);
            if (elems.Count % 2 != 0)
                return null!;

            var dictionary = (IDictionary)type.GetConstructor([typeof(int)]).Invoke([elems.Count / 2]);
            for (int i = 0; i < elems.Count; i += 2)
            {
                if (elems[i].Length <= 2)
                    continue;
                string keyValue = elems[i].Substring(1, elems[i].Length - 2);
                object val = ParseValue(valueType, elems[i + 1]);
                dictionary[keyValue] = val;
            }
            return dictionary;
        }
        if (type == typeof(object))
        {
            return ParseAnonymousValue(json);
        }
        if (json[0] == '{' && json[json.Length - 1] == '}')
        {
            return ParseObject(type, json);
        }

        return null!;
    }

    static object ParseAnonymousValue(string json)
    {
        if (json.Length == 0)
            return null!;
        if (json[0] == '{' && json[json.Length - 1] == '}')
        {
            List<string> elems = Split(json);
            if (elems.Count % 2 != 0)
                return null!;
            var dict = new Dictionary<string, object>(elems.Count / 2);
            for (int i = 0; i < elems.Count; i += 2)
                dict[elems[i].Substring(1, elems[i].Length - 2)] = ParseAnonymousValue(elems[i + 1]);
            return dict;
        }
        if (json[0] == '[' && json[json.Length - 1] == ']')
        {
            List<string> items = Split(json);
            var finalList = new List<object>(items.Count);
            for (int i = 0; i < items.Count; i++)
                finalList.Add(ParseAnonymousValue(items[i]));
            return finalList;
        }
        if (json[0] == '"' && json[json.Length - 1] == '"')
        {
            string str = json.Substring(1, json.Length - 2);
            return str.Replace("\\", string.Empty);
        }
        if (char.IsDigit(json[0]) || json[0] == '-')
        {
            if (json.Contains("."))
            {
                double.TryParse(json, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double result);
                return result;
            }
            else
            {
                int.TryParse(json, out int result);
                return result;
            }
        }
        if (json == "true")
            return true;
        if (json == "false")
            return false;
        // handles json == "null" as well as invalid JSON
        return null!;
    }

    static Dictionary<string, T> CreateMemberNameDictionary<T>(T[] members) where T : MemberInfo
    {
        Dictionary<string, T> nameToMember = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < members.Length; i++)
        {
            T member = members[i];
            if (member.IsDefined(typeof(IgnoreDataMemberAttribute), true))
                continue;

            string name = member.Name;
            if (member.IsDefined(typeof(DataMemberAttribute), true))
            {
                DataMemberAttribute dataMemberAttribute = (DataMemberAttribute)Attribute.GetCustomAttribute(member, typeof(DataMemberAttribute), true);
                if (!string.IsNullOrEmpty(dataMemberAttribute.Name))
                    name = dataMemberAttribute.Name;
            }

            nameToMember.Add(name, member);
        }

        return nameToMember;
    }

    static object ParseObject(Type type, string json)
    {
        object instance = FormatterServices.GetUninitializedObject(type);

        //The list is split into key/value pairs only, this means the split must be divisible by 2 to be valid JSON
        List<string> elems = Split(json);
        if (elems.Count % 2 != 0)
            return instance;

        if (!fieldInfoCache.TryGetValue(type, out Dictionary<string, FieldInfo> nameToField))
        {
            nameToField = CreateMemberNameDictionary(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy));
            fieldInfoCache.Add(type, nameToField);
        }
        if (!propertyInfoCache.TryGetValue(type, out Dictionary<string, PropertyInfo> nameToProperty))
        {
            nameToProperty = CreateMemberNameDictionary(type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy));
            propertyInfoCache.Add(type, nameToProperty);
        }

        for (int i = 0; i < elems.Count; i += 2)
        {
            if (elems[i].Length <= 2)
                continue;
            string key = elems[i].Substring(1, elems[i].Length - 2);
            string value = elems[i + 1];

            if (nameToField.TryGetValue(key, out FieldInfo fieldInfo))
                fieldInfo.SetValue(instance, ParseValue(fieldInfo.FieldType, value));
            else if (nameToProperty.TryGetValue(key, out PropertyInfo propertyInfo))
                propertyInfo.SetValue(instance, ParseValue(propertyInfo.PropertyType, value), null);
        }

        return instance;
    }
}
