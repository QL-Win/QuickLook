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

namespace QuickLook.Plugin.AppViewer.ApkPackageParser;

/// <summary>
/// https://github.com/hylander0/Iteedee.ApkReader
/// </summary>
public class ApkManifest
{
    private string result = string.Empty;

    private bool isUtf8;

    // decompressXML -- Parse the 'compressed' binary form of Android XML docs
    // such as for AndroidManifest.xml in .apk files
    private const int startDocTag = 0x00100100;

    private const int endDocTag = 0x00100101;
    private const int startTag = 0x00100102;
    private const int endTag = 0x00100103;
    private const int textTag = 0x00100104;

    public string ReadManifestFileIntoXml(byte[] manifestFileData)
    {
        if (manifestFileData.Length == 0)
            throw new Exception("Failed to read manifest data.  Byte array was empty");
        // Compressed XML file/bytes starts with 24x bytes of data,
        // 9 32 bit words in little endian order (LSB first):
        //   0th word is 03 00 08 00
        //   3rd word SEEMS TO BE:  Offset at then of StringTable
        //   4th word is: Number of strings in string table
        // WARNING: Sometime I indiscriminently display or refer to word in
        //   little endian storage format, or in integer format (ie MSB first).
        int numbStrings = LEW(manifestFileData, 4 * 4);

        // StringIndexTable starts at offset 24x, an array of 32 bit LE offsets
        // of the length/string data in the StringTable.
        int sitOff = 0x24;  // Offset of start of StringIndexTable

        // StringTable, each string is represented with a 16 bit little endian
        // character count, followed by that number of 16 bit (LE) (Unicode) chars.
        int stOff = sitOff + numbStrings * 4;  // StringTable follows StrIndexTable

        // XMLTags, The XML tag tree starts after some unknown content after the
        // StringTable.  There is some unknown data after the StringTable, scan
        // forward from this point to the flag for the start of an XML start tag.
        int xmlTagOff = LEW(manifestFileData, 3 * 4);  // Start from the offset in the 3rd word.
                                                       // Scan forward until we find the bytes: 0x02011000(x00100102 in normal int)

        // String pool is encoded in UTF-8
        // https://android.googlesource.com/platform/frameworks/base/+/master/libs/androidfw/include/androidfw/ResourceTypes.h#451
        int flag = LEW(manifestFileData, 4 * 6);
        this.isUtf8 = (flag & (1 << 8)) > 0;

        for (int ii = xmlTagOff; ii < manifestFileData.Length - 4; ii += 4)
        {
            if (LEW(manifestFileData, ii) == startTag)
            {
                xmlTagOff = ii; break;
            }
        } // end of hack, scanning for start of first start tag

        // XML tags and attributes:
        // Every XML start and end tag consists of 6 32 bit words:
        //   0th word: 02011000 for startTag and 03011000 for endTag
        //   1st word: a flag?, like 38000000
        //   2nd word: Line of where this tag appeared in the original source file
        //   3rd word: FFFFFFFF ??
        //   4th word: StringIndex of NameSpace name, or FFFFFFFF for default NS
        //   5th word: StringIndex of Element Name
        //   (Note: 01011000 in 0th word means end of XML document, endDocTag)

        // Start tags (not end tags) contain 3 more words:
        //   6th word: 14001400 meaning??
        //   7th word: Number of Attributes that follow this tag(follow word 8th)
        //   8th word: 00000000 meaning??

        // Attributes consist of 5 words:
        //   0th word: StringIndex of Attribute Name's Namespace, or FFFFFFFF
        //   1st word: StringIndex of Attribute Name
        //   2nd word: StringIndex of Attribute Value, or FFFFFFF if ResourceId used
        //   3rd word: Flags?
        //   4th word: str ind of attr value again, or ResourceId of value

        // TMP, dump string table to tr for debugging
        //tr.addSelect("strings", null);
        //for (int ii=0; ii<numbStrings; ii++) {
        //  // Length of string starts at StringTable plus offset in StrIndTable
        //  String str = CompXmlString(xml, sitOff, stOff, ii);
        //  tr.add(String.valueOf(ii), str);
        //}
        //tr.parent();

        // Step through the XML tree element tags and attributes
        int off = xmlTagOff;
        int indent = 0;
        int startTagLineNo = -2;
        int startDocTagCounter = 1;
        while (off < manifestFileData.Length)
        {
            int tag0 = LEW(manifestFileData, off);
            //int tag1 = LEW(manifestFileData, off+1*4);
            int lineNo = LEW(manifestFileData, off + 2 * 4);
            //int tag3 = LEW(manifestFileData, off+3*4);
            int nameNsSi = LEW(manifestFileData, off + 4 * 4);
            int nameSi = LEW(manifestFileData, off + 5 * 4);

            if (tag0 == startTag)
            { // XML START TAG
                int tag6 = LEW(manifestFileData, off + 6 * 4);  // Expected to be 14001400
                int numbAttrs = LEW(manifestFileData, off + 7 * 4);  // Number of Attributes to follow
                //int tag8 = LEW(manifestFileData, off+8*4);  // Expected to be 00000000
                off += 9 * 4;  // Skip over 6+3 words of startTag data
                string name = CompXmlString(manifestFileData, sitOff, stOff, nameSi);
                //tr.addSelect(name, null);
                startTagLineNo = lineNo;

                // Look for the Attributes

                string sb = string.Empty;
                for (int ii = 0; ii < numbAttrs; ii++)
                {
                    int attrNameNsSi = LEW(manifestFileData, off);  // AttrName Namespace Str Ind, or FFFFFFFF
                    int attrNameSi = LEW(manifestFileData, off + 1 * 4);  // AttrName String Index
                    int attrValueSi = LEW(manifestFileData, off + 2 * 4); // AttrValue Str Ind, or FFFFFFFF
                    int attrFlags = LEW(manifestFileData, off + 3 * 4);
                    int attrResId = LEW(manifestFileData, off + 4 * 4);  // AttrValue ResourceId or dup AttrValue StrInd
                    off += 5 * 4;  // Skip over the 5 words of an attribute

                    string attrName = CompXmlString(manifestFileData, sitOff, stOff, attrNameSi);
                    string attrValue = attrValueSi != -1
                      ? CompXmlString(manifestFileData, sitOff, stOff, attrValueSi)
                      : /*"resourceID 0x" + */attrResId.ToString();
                    sb += " " + attrName + "=\"" + attrValue + "\"";
                    //tr.add(attrName, attrValue);
                }
                PrtIndent(indent, "<" + name + sb + ">");
                indent++;
            }
            else if (tag0 == endTag)
            {
                // XML END TAG
                indent--;
                off += 6 * 4;  // Skip over 6 words of endTag data
                string name = CompXmlString(manifestFileData, sitOff, stOff, nameSi);
                PrtIndent(indent, "</" + name + ">  \r\n"/*+"(line " + startTagLineNo + "-" + lineNo + ")"*/);
                //tr.parent();  // Step back up the NobTree
            }
            else if (tag0 == startDocTag)
            {
                startDocTagCounter++;
                off += 4;
            }
            else if (tag0 == endDocTag)
            {
                // END OF XML DOC TAG
                startDocTagCounter--;
                if (startDocTagCounter == 0)
                    break;
            }
            else if (tag0 == textTag)
            {
                // code "copied" https://github.com/mikandi/php-apk-parser/blob/fixed-mikandi-versionName/lib/ApkParser/XmlParser.php
                uint sentinal = 0xffffffff;
                while (off < manifestFileData.Length)
                {
                    uint curr = (uint)LEW(manifestFileData, off);
                    off += 4;
                    if (off > manifestFileData.Length)
                    {
                        throw new Exception("Sentinal not found before end of file");
                    }
                    if (curr == sentinal && sentinal == 0xffffffff)
                    {
                        sentinal = 0x00000000;
                    }
                    else if (curr == sentinal)
                    {
                        break;
                    }
                }
            }
            else
            {
                Prt("  Unrecognized tag code '" + tag0.ToString("X")
                  + "' at offset " + off);
                break;
            }
        } // end of while loop scanning tags and attributes of XML tree
        //Prt("    end at offset " + off);

        return result;
    } // end of decompressXML

    public string CompXmlString(byte[] xml, int sitOff, int stOff, int strInd)
    {
        if (strInd < 0) return null;
        int strOff = stOff + LEW(xml, sitOff + strInd * 4);
        return CompXmlStringAt(xml, strOff);
    }

    public static string spaces = "                                             ";

    public void PrtIndent(int indent, string str)
    {
        Prt(spaces.Substring(0, Math.Min(indent * 2, spaces.Length)) + str);
    }

    private void Prt(string p)
    {
        result += p;
    }

    // CompXmlStringAt -- Return the string stored in StringTable format at
    // offset strOff.  This offset points to the 16 bit string length, which
    // is followed by that number of 16 bit (Unicode) chars.
    public string CompXmlStringAt(byte[] arr, int strOff)
    {
        /**
         * Strings in UTF-8 format have length indicated by a length encoded in the
         * stored data. It is either 1 or 2 characters of length data. This allows a
         * maximum length of 0x7FFF (32767 bytes), but you should consider storing
         * text in another way if you're using that much data in a single string.
         *
         * If the high bit is set, then there are two characters or 2 bytes of length
         * data encoded. In that case, drop the high bit of the first character and
         * add it together with the next character.
         * https://android.googlesource.com/platform/frameworks/base/+/master/libs/androidfw/ResourceTypes.cpp#674
         */
        int strLen = arr[strOff];
        if ((strLen & 0x80) != 0)
            strLen = ((strLen & 0x7f) << 8) + arr[strOff + 1];

        if (!isUtf8)
            strLen *= 2;

        byte[] chars = new byte[strLen];
        for (int ii = 0; ii < strLen; ii++)
        {
            chars[ii] = arr[strOff + 2 + ii];
        }

        return System.Text.Encoding.GetEncoding(isUtf8 ? "UTF-8" : "UTF-16").GetString(chars);
    } // end of CompXmlStringAt

    // LEW -- Return value of a Little Endian 32 bit word from the byte array
    //   at offset off.
    public int LEW(byte[] arr, int off)
    {
        //return (int)(arr[off + 3] << 24 & 0xff000000 | arr[off + 2] << 16 & 0xff0000 | arr[off + 1] << 8 & 0xff00 | arr[off] & 0xFF);
        return (int)(((uint)arr[off + 3]) << 24 & 0xff000000 | ((uint)arr[off + 2]) << 16 & 0xff0000 | ((uint)arr[off + 1]) << 8 & 0xff00 | ((uint)arr[off]) & 0xFF);
    } // end of LEW
}
