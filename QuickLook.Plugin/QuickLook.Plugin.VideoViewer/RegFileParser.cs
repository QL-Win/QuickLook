using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace RegFileParser;

/// <summary>
///     The main reg file parsing class.
///     Reads the given reg file and stores the content as
///     a Dictionary of registry keys and values as a Dictionary of registry values <see cref="RegValueObject" />
/// </summary>
public class RegFileObject
{
    #region Public Properties

    /// <summary>
    ///     Gets the dictionary containing all entries
    /// </summary>
    public Dictionary<string, Dictionary<string, RegValueObject>> RegValues => regvalues;

    #endregion Public Properties

    #region Private Fields

    /// <summary>
    ///     Raw content of the reg file
    /// </summary>
    private string content;

    /// <summary>
    ///     the dictionary containing parsed registry values
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, RegValueObject>> regvalues;

    #endregion Private Fields

    #region Constructors

    public RegFileObject()
    {
        regvalues = new Dictionary<string, Dictionary<string, RegValueObject>>();
    }

    public RegFileObject(string RegFileName)
    {
        regvalues = new Dictionary<string, Dictionary<string, RegValueObject>>();
        Read(RegFileName);
    }

    public RegFileObject(byte[] RegFileContents)
    {
        regvalues = new Dictionary<string, Dictionary<string, RegValueObject>>();
        Read(RegFileContents);
    }

    #endregion Constructors

    #region Private Methods

    /// <summary>
    ///     Imports the reg file
    /// </summary>
    public void Read(string path)
    {
        if (File.Exists(path))
            Read(File.ReadAllBytes(path));
    }

    /// <summary>
    ///     Imports the reg file
    /// </summary>
    public void Read(byte[] bytes)
    {
        Dictionary<string, Dictionary<string, string>> normalizedContent = null;

        content = Encoding.Unicode.GetString(bytes);

        try
        {
            normalizedContent = ParseFile();
        }
        catch (Exception ex)
        {
            throw new Exception("Error reading reg file.", ex);
        }

        if (normalizedContent == null)
            throw new Exception("Error normalizing reg file content.");

        foreach (var entry in normalizedContent)
        {
            var regValueList = new Dictionary<string, RegValueObject>();

            foreach (var item in entry.Value)
                try
                {
                    regValueList.Add(item.Key, new RegValueObject(entry.Key, item.Key, item.Value, "UTF8"));
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Exception thrown on processing string {0}", item), ex);
                }

            regvalues.Add(entry.Key, regValueList);
        }
    }

    /// <summary>
    ///     Parses the reg file for reg keys and reg values
    /// </summary>
    /// <returns>A Dictionary with reg keys as Dictionary keys and a Dictionary of (valuename, valuedata)</returns>
    private Dictionary<string, Dictionary<string, string>> ParseFile()
    {
        var retValue = new Dictionary<string, Dictionary<string, string>>();

        try
        {
            //Get registry keys and values content string
            //Change proposed by Jenda27
            //Dictionary<String, String> dictKeys = NormalizeDictionary("^[\t ]*\\[.+\\]\r\n", content, true);
            var dictKeys = NormalizeKeysDictionary(content);

            //Get registry values for a given key
            foreach (var item in dictKeys)
            {
                if (string.IsNullOrEmpty(item.Value)) continue;
                //Dictionary<String, String> dictValues = NormalizeDictionary("^[\t ]*(\".+\"|@)=", item.Value, false);
                var dictValues = NormalizeValuesDictionary(item.Value);
                retValue.Add(item.Key, dictValues);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Exception thrown on parsing reg file.", ex);
        }

        return retValue;
    }

    /// <summary>
    ///     Creates a flat Dictionary using given searcn pattern
    /// </summary>
    /// <param name="content">The content string to be parsed</param>
    /// <returns>A Dictionary with retrieved keys and remaining content</returns>
    private Dictionary<string, string> NormalizeKeysDictionary(string content)
    {
        var searchPattern = "^[\t ]*\\[.+\\][\r\n]+";
        var matches = Regex.Matches(content, searchPattern, RegexOptions.Multiline);

        var startIndex = 0;
        var lengthIndex = 0;
        var dictKeys = new Dictionary<string, string>();

        foreach (Match match in matches)
            try
            {
                //Retrieve key
                var sKey = match.Value;
                //change proposed by Jenda27
                //if (sKey.EndsWith("\r\n")) sKey = sKey.Substring(0, sKey.Length - 2);
                while (sKey.EndsWith("\r\n")) sKey = sKey.Substring(0, sKey.Length - 2);
                if (sKey.EndsWith("=")) sKey = sKey.Substring(0, sKey.Length - 1);
                sKey = StripeBraces(sKey);
                if (sKey == "@")
                    sKey = "";
                else
                    sKey = StripeLeadingChars(sKey, "\"");

                //Retrieve value
                startIndex = match.Index + match.Length;
                var nextMatch = match.NextMatch();
                lengthIndex = (nextMatch.Success ? nextMatch.Index : content.Length) - startIndex;
                var sValue = content.Substring(startIndex, lengthIndex);
                //Removing the ending CR
                //change suggested by Jenda27
                //if (sValue.EndsWith("\r\n")) sValue = sValue.Substring(0, sValue.Length - 2);
                while (sValue.EndsWith("\r\n")) sValue = sValue.Substring(0, sValue.Length - 2);
                //fix for the double key names issue
                //dictKeys.Add(sKey, sValue);
                if (dictKeys.ContainsKey(sKey))
                {
                    var tmpcontent = dictKeys[sKey];
                    var tmpsb = new StringBuilder(tmpcontent);
                    if (!tmpcontent.EndsWith(Environment.NewLine)) tmpsb.AppendLine();
                    tmpsb.Append(sValue);
                    dictKeys[sKey] = tmpsb.ToString();
                }
                else
                {
                    dictKeys.Add(sKey, sValue);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Exception thrown on processing string {0}", match.Value), ex);
            }

        return dictKeys;
    }

    /// <summary>
    ///     Creates a flat Dictionary using given searcn pattern
    /// </summary>
    /// <param name="content">The content string to be parsed</param>
    /// <returns>A Dictionary with retrieved keys and remaining content</returns>
    private Dictionary<string, string> NormalizeValuesDictionary(string content)
    {
        var searchPattern = @"^[\t ]*("".+""|@)=(""[^""]*""|[^""]+)";
        var matches = Regex.Matches(content, searchPattern, RegexOptions.Multiline);

        var dictKeys = new Dictionary<string, string>();

        foreach (Match match in matches)
            try
            {
                //Retrieve key
                var sKey = match.Groups[1].Value;

                //Retrieve value
                var sValue = match.Groups[2].Value;

                //Removing the ending CR
                while (sKey.EndsWith("\r\n")) sKey = sKey.Substring(0, sKey.Length - 2);

                if (sKey == "@")
                    sKey = "";
                else
                    sKey = StripeLeadingChars(sKey, "\"");

                while (sValue.EndsWith("\r\n")) sValue = sValue.Substring(0, sValue.Length - 2);

                if (dictKeys.ContainsKey(sKey))
                {
                    var tmpcontent = dictKeys[sKey];
                    var tmpsb = new StringBuilder(tmpcontent);
                    if (!tmpcontent.EndsWith(Environment.NewLine)) tmpsb.AppendLine();
                    tmpsb.Append(sValue);
                    dictKeys[sKey] = tmpsb.ToString();
                }
                else
                {
                    dictKeys.Add(sKey, sValue);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Exception thrown on processing string {0}", match.Value), ex);
            }

        return dictKeys;
    }

    /// <summary>
    ///     Removes the leading and ending characters from the given string
    /// </summary>
    /// <param name="sLine">given string</param>
    /// <returns>edited string</returns>
    /// <remarks></remarks>
    private string StripeLeadingChars(string sLine, string leadChar)
    {
        var tmpvalue = sLine.Trim();
        if (tmpvalue.StartsWith(leadChar) & tmpvalue.EndsWith(leadChar))
            return tmpvalue.Substring(1, tmpvalue.Length - 2);
        return tmpvalue;
    }

    /// <summary>
    ///     Removes the leading and ending parenthesis from the given string
    /// </summary>
    /// <param name="sLine">given string</param>
    /// <returns>edited string</returns>
    /// <remarks></remarks>
    private string StripeBraces(string sLine)
    {
        var tmpvalue = sLine.Trim();
        if (tmpvalue.StartsWith("[") & tmpvalue.EndsWith("]")) return tmpvalue.Substring(1, tmpvalue.Length - 2);
        return tmpvalue;
    }

    #endregion Private Methods
}

[Serializable]
public class RegValueObject
{
    private string entry;
    private string parentkey;
    private string parentkeywithoutroot;
    private string root;
    private string type;
    private string value;

    /// <summary>
    ///     Parameterless constructor
    /// </summary>
    public RegValueObject()
    {
        root = string.Empty;
        parentkey = string.Empty;
        parentkeywithoutroot = string.Empty;
        entry = string.Empty;
        value = string.Empty;
        type = string.Empty;
    }

    /// <summary>
    ///     Overloaded constructor
    /// </summary>
    /// <param name="propertyString">A line from the [Registry] section of the *.sig signature file</param>
    public RegValueObject(string regKeyName, string regValueName, string regValueData, string encoding)
    {
        parentkey = regKeyName.Trim();
        parentkeywithoutroot = parentkey;
        root = GetHive(ref parentkeywithoutroot);
        entry = regValueName;
        value = regValueData;
        type = string.Empty;
        var tmpStringValue = value;
        type = GetRegEntryType(ref tmpStringValue, encoding);
        value = tmpStringValue;
    }

    #region Public Methods

    /// <summary>
    ///     Overriden Method
    /// </summary>
    /// <returns>An entry for the [Registry] section of the *.sig signature file</returns>
    public override string ToString()
    {
        return string.Format("{0}\\\\{1}={2}{3}", parentkey, entry, SetRegEntryType(type), value);
    }

    #endregion Public Methods

    #region Public Properties

    /// <summary>
    ///     Regsitry value name
    /// </summary>
    [XmlElement("entry", typeof(string))]
    public string Entry
    {
        get => entry;
        set => entry = value;
    }

    /// <summary>
    ///     Registry value parent key
    /// </summary>
    [XmlElement("key", typeof(string))]
    public string ParentKey
    {
        get => parentkey;
        set
        {
            parentkey = value;
            parentkeywithoutroot = parentkey;
            root = GetHive(ref parentkeywithoutroot);
        }
    }

    /// <summary>
    ///     Registry value root hive
    /// </summary>
    [XmlElement("root", typeof(string))]
    public string Root
    {
        get => root;
        set => root = value;
    }

    /// <summary>
    ///     Registry value type
    /// </summary>
    [XmlElement("type", typeof(string))]
    public string Type
    {
        get => type;
        set => type = value;
    }

    /// <summary>
    ///     Registry value data
    /// </summary>
    [XmlElement("value", typeof(string))]
    public string Value
    {
        get => value;
        set => this.value = value;
    }

    [XmlElement("value", typeof(string))]
    public string ParentKeyWithoutRoot
    {
        get => parentkeywithoutroot;
        set => parentkeywithoutroot = value;
    }

    #endregion Public Properties

    #region Private Functions

    private string GetHive(ref string skey)
    {
        var tmpLine = skey.Trim();

        if (tmpLine.StartsWith("HKEY_LOCAL_MACHINE"))
        {
            skey = skey.Substring(18);
            if (skey.StartsWith("\\")) skey = skey.Substring(1);
            return "HKEY_LOCAL_MACHINE";
        }

        if (tmpLine.StartsWith("HKEY_CLASSES_ROOT"))
        {
            skey = skey.Substring(17);
            if (skey.StartsWith("\\")) skey = skey.Substring(1);
            return "HKEY_CLASSES_ROOT";
        }

        if (tmpLine.StartsWith("HKEY_USERS"))
        {
            skey = skey.Substring(10);
            if (skey.StartsWith("\\")) skey = skey.Substring(1);
            return "HKEY_USERS";
        }

        if (tmpLine.StartsWith("HKEY_CURRENT_CONFIG"))
        {
            skey = skey.Substring(19);
            if (skey.StartsWith("\\")) skey = skey.Substring(1);
            return "HKEY_CURRENT_CONFIG";
        }

        if (tmpLine.StartsWith("HKEY_CURRENT_USER"))
        {
            skey = skey.Substring(17);
            if (skey.StartsWith("\\")) skey = skey.Substring(1);
            return "HKEY_CURRENT_USER";
        }

        return "";
    }

    /// <summary>
    ///     Retrieves the reg value type, parsing the prefix of the value
    /// </summary>
    /// <param name="sTextLine">Registry value row string</param>
    /// <returns>Value</returns>
    private string GetRegEntryType(ref string sTextLine, string textEncoding)
    {
        if (sTextLine.StartsWith("hex(a):"))
        {
            sTextLine = sTextLine.Substring(7);
            return "REG_RESOURCE_REQUIREMENTS_LIST";
        }

        if (sTextLine.StartsWith("hex(b):"))
        {
            sTextLine = sTextLine.Substring(7);
            return "REG_QWORD";
        }

        if (sTextLine.StartsWith("dword:"))
        {
            sTextLine = Convert.ToInt32(sTextLine.Substring(6), 16).ToString();
            return "REG_DWORD";
        }

        if (sTextLine.StartsWith("hex(7):"))
        {
            sTextLine = StripeContinueChar(sTextLine.Substring(7));
            sTextLine = GetStringRepresentation(sTextLine.Split(','), textEncoding);
            return "REG_MULTI_SZ";
        }

        if (sTextLine.StartsWith("hex(6):"))
        {
            sTextLine = StripeContinueChar(sTextLine.Substring(7));
            sTextLine = GetStringRepresentation(sTextLine.Split(','), textEncoding);
            return "REG_LINK";
        }

        if (sTextLine.StartsWith("hex(2):"))
        {
            sTextLine = StripeContinueChar(sTextLine.Substring(7));
            sTextLine = GetStringRepresentation(sTextLine.Split(','), textEncoding);
            return "REG_EXPAND_SZ";
        }

        if (sTextLine.StartsWith("hex(0):"))
        {
            sTextLine = sTextLine.Substring(7);
            return "REG_NONE";
        }

        if (sTextLine.StartsWith("hex:"))
        {
            sTextLine = StripeContinueChar(sTextLine.Substring(4));
            if (sTextLine.EndsWith(",")) sTextLine = sTextLine.Substring(0, sTextLine.Length - 1);
            return "REG_BINARY";
        }

        sTextLine = Regex.Unescape(sTextLine);
        sTextLine = StripeLeadingChars(sTextLine, "\"");
        return "REG_SZ";
    }

    private string SetRegEntryType(string sRegDataType)
    {
        switch (sRegDataType)
        {
            case "REG_QWORD":
                return "hex(b):";

            case "REG_RESOURCE_REQUIREMENTS_LIST":
                return "hex(a):";

            case "REG_FULL_RESOURCE_DESCRIPTOR":
                return "hex(9):";

            case "REG_RESOURCE_LIST":
                return "hex(8):";

            case "REG_MULTI_SZ":
                return "hex(7):";

            case "REG_LINK":
                return "hex(6):";

            case "REG_DWORD":
                return "dword:";

            case "REG_EXPAND_SZ":
                return "hex(2):";

            case "REG_NONE":
                return "hex(0):";

            case "REG_BINARY":
                return "hex:";

            case "REG_SZ":
                return "";

            default:
                return "";
        }

        /*
        hex: REG_BINARY
        hex(0): REG_NONE
        hex(1): REG_SZ
        hex(2): EXPAND_SZ
        hex(3): REG_BINARY
        hex(4): REG_DWORD
        hex(5): REG_DWORD_BIG_ENDIAN ; invalid type ?
        hex(6): REG_LINK
        hex(7): REG_MULTI_SZ
        hex(8): REG_RESOURCE_LIST
        hex(9): REG_FULL_RESOURCE_DESCRIPTOR
        hex(a): REG_RESOURCE_REQUIREMENTS_LIST
        hex(b): REG_QWORD
        */
    }

    /// <summary>
    ///     Removes the leading and ending characters from the given string
    /// </summary>
    /// <param name="sline">given string</param>
    /// <returns>edited string</returns>
    /// <remarks></remarks>
    private string StripeLeadingChars(string sline, string LeadChar)
    {
        var tmpvalue = sline.Trim();
        if (tmpvalue.StartsWith(LeadChar) & tmpvalue.EndsWith(LeadChar))
            return tmpvalue.Substring(1, tmpvalue.Length - 2);
        return tmpvalue;
    }

    /// <summary>
    ///     Removes the leading and ending parenthesis from the given string
    /// </summary>
    /// <param name="sline">given string</param>
    /// <returns>edited string</returns>
    /// <remarks></remarks>
    private string StripeBraces(string sline)
    {
        var tmpvalue = sline.Trim();
        if (tmpvalue.StartsWith("[") & tmpvalue.EndsWith("]")) return tmpvalue.Substring(1, tmpvalue.Length - 2);
        return tmpvalue;
    }

    /// <summary>
    ///     Removes the ending backslashes from the given string
    /// </summary>
    /// <param name="sline">given string</param>
    /// <returns>edited string</returns>
    /// <remarks></remarks>
    private string StripeContinueChar(string sline)
    {
        var tmpString = Regex.Replace(sline, "\\\\\r\n[ ]*", string.Empty);
        return tmpString;
    }

    /// <summary>
    ///     Converts the byte arrays (saved as array of string) into string
    /// </summary>
    /// <param name="stringArray">Array of string</param>
    /// <returns>String value</returns>
    private string GetStringRepresentation(string[] stringArray, string encoding)
    {
        if (stringArray.Length > 1)
        {
            var sb = new StringBuilder();

            if (encoding == "UTF8")
                for (var i = 0; i < stringArray.Length - 2; i += 2)
                {
                    var tmpCharacter = stringArray[i + 1] + stringArray[i];
                    if (tmpCharacter == "0000")
                    {
                        sb.Append(Environment.NewLine);
                    }
                    else
                    {
                        var tmpChar = Convert.ToChar(Convert.ToInt32(tmpCharacter, 16));
                        sb.Append(tmpChar);
                    }
                }
            else
                for (var i = 0; i < stringArray.Length - 1; i += 1)
                    if (stringArray[i] == "00")
                    {
                        sb.Append(Environment.NewLine);
                    }
                    else
                    {
                        var tmpChar = Convert.ToChar(Convert.ToInt32(stringArray[i], 16));
                        sb.Append(tmpChar);
                    }

            return sb.ToString();
        }

        return string.Empty;
    }

    #endregion Private Functions
}
