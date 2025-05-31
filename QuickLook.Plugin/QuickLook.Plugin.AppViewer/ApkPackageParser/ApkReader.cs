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
using System.Xml;

namespace QuickLook.Plugin.AppViewer.ApkPackageParser;

/// <summary>
/// https://github.com/hylander0/Iteedee.ApkReader
/// </summary>
public class ApkReader
{
    private const int VER_ID = 0;
    private const int ICN_ID = 1;
    private const int LABEL_ID = 2;
    private readonly string[] VER_ICN = new string[3];

    // Some possible tags and attributes
    private readonly string[] TAGS = ["manifest", "application", "activity"];

    public string FuzzFindInDocument(XmlDocument doc, string tag, string attr)
    {
        foreach (string t in TAGS)
        {
            XmlNodeList nodelist = doc.GetElementsByTagName(t);
            for (int i = 0; i < nodelist.Count; i++)
            {
                XmlNode element = nodelist.Item(i);
                if (element.NodeType == XmlNodeType.Element)
                {
                    XmlAttributeCollection map = element.Attributes;
                    for (int j = 0; j < map.Count; j++)
                    {
                        XmlNode element2 = map.Item(j);
                        if (element2.Name.EndsWith(attr))
                        {
                            return element2.Value;
                        }
                    }
                }
            }
        }
        return null;
    }

    private void ExtractPermissions(ApkInfo info, XmlDocument doc)
    {
        ExtractPermission(info, doc, "uses-permission", "name");
        ExtractPermission(info, doc, "permission-group", "name");
        ExtractPermission(info, doc, "service", "permission");
        ExtractPermission(info, doc, "provider", "permission");
        ExtractPermission(info, doc, "activity", "permission");
    }

    private bool ReadBoolean(XmlDocument doc, string tag, string attribute)
    {
        try
        {
            string str = FindInDocument(doc, tag, attribute);
            return Convert.ToBoolean(str);
        }
        catch
        {
        }
        return false;
    }

    private void ExtractSupportScreens(ApkInfo info, XmlDocument doc)
    {
        info.SupportSmallScreens = ReadBoolean(doc, "supports-screens", "android:smallScreens");
        info.SupportNormalScreens = ReadBoolean(doc, "supports-screens", "android:normalScreens");
        info.SupportLargeScreens = ReadBoolean(doc, "supports-screens", "android:largeScreens");

        if (info.SupportSmallScreens || info.SupportNormalScreens || info.SupportLargeScreens)
            info.SupportAnyDensity = false;
    }

    public ApkInfo ExtractInfo(byte[] manifest_xml, byte[] resources_arsx)
    {
        string manifestXml;
        ApkManifest manifest = new();
        try
        {
            manifestXml = manifest.ReadManifestFileIntoXml(manifest_xml);
        }
        catch (Exception ex)
        {
            throw ex;
        }

        XmlDocument doc = new();
        doc.LoadXml(manifestXml);
        return ExtractInfo(doc, resources_arsx);
    }

    public ApkInfo ExtractInfo(XmlDocument manifestXml, byte[] resources_arsx)
    {
        ApkInfo info = new();
        VER_ICN[VER_ID] = string.Empty;
        VER_ICN[ICN_ID] = string.Empty;
        VER_ICN[LABEL_ID] = string.Empty;
        try
        {
            XmlDocument doc = manifestXml ?? throw new Exception("Document initialize failed");

            // Fill up the permission field
            ExtractPermissions(info, doc);

            // Fill up some basic fields
            info.MinSdkVersion = FindInDocument(doc, "uses-sdk", "minSdkVersion");
            info.TargetSdkVersion = FindInDocument(doc, "uses-sdk", "targetSdkVersion");
            info.VersionCode = FindInDocument(doc, "manifest", "versionCode");
            info.VersionName = FindInDocument(doc, "manifest", "versionName");
            info.PackageName = FindInDocument(doc, "manifest", "package");

            info.Label = FindInDocument(doc, "application", "label");
            if (info.Label.StartsWith("@"))
                VER_ICN[LABEL_ID] = info.Label;
            else if (int.TryParse(info.Label, out int labelID))
                VER_ICN[LABEL_ID] = string.Format("@{0}", labelID.ToString("X4"));

            // Get the value of android:Debuggable in the manifest
            // "0" = false and "-1" = true
            info.Debuggable = FindInDocument(doc, "application", "debuggable");

            // Fill up the support screen field
            ExtractSupportScreens(info, doc);

            info.VersionCode ??= FuzzFindInDocument(doc, "manifest", "versionCode");

            if (info.VersionName == null)
                info.VersionName = FuzzFindInDocument(doc, "manifest", "versionName");
            else if (info.VersionName.StartsWith("@"))
                VER_ICN[VER_ID] = info.VersionName;

            string id = FindInDocument(doc, "application", "android:icon");
            if (null == id)
            {
                id = FuzzFindInDocument(doc, "manifest", "icon");
            }

            if (null == id)
            {
                Debug.WriteLine("icon resId Not Found!");
                return info;
            }

            // Find real strings
            if (!info.HasIcon && id != null)
            {
                if (id.StartsWith("@android:"))
                    VER_ICN[ICN_ID] = "@" + id.Substring("@android:".Length);
                else
                    VER_ICN[ICN_ID] = string.Format("@{0}", Convert.ToInt32(id).ToString("X4"));

                List<string> resId = [];

                for (int i = 0; i < VER_ICN.Length; i++)
                {
                    if (VER_ICN[i].StartsWith("@"))
                        resId.Add(VER_ICN[i]);
                }

                ApkResourceFinder finder = new();
                info.ResStrings = finder.ProcessResourceTable(resources_arsx, resId);

                if (!VER_ICN[VER_ID].Equals(string.Empty))
                {
                    List<string> versions = null;
                    if (info.ResStrings.ContainsKey(VER_ICN[VER_ID].ToUpper()))
                        versions = info.ResStrings[VER_ICN[VER_ID].ToUpper()];
                    if (versions != null)
                    {
                        if (versions.Count > 0)
                            info.VersionName = versions[0];
                    }
                    else
                    {
                        throw new Exception("VersionName Cant Find in resource with id " + VER_ICN[VER_ID]);
                    }
                }

                List<string> iconPaths = null;
                if (info.ResStrings.ContainsKey(VER_ICN[ICN_ID].ToUpper()))
                    iconPaths = info.ResStrings[VER_ICN[ICN_ID].ToUpper()];
                if (iconPaths != null && iconPaths.Count > 0)
                {
                    info.IconFileName = [];
                    foreach (string iconFileName in iconPaths)
                    {
                        if (iconFileName != null)
                        {
                            if (iconFileName.Contains(@"/"))
                            {
                                info.IconFileName.Add(iconFileName);
                                info.HasIcon = true;
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("Icon Cant Find in resource with id " + VER_ICN[ICN_ID]);
                }

                if (!VER_ICN[LABEL_ID].Equals(string.Empty))
                {
                    List<string> labels = null;
                    if (info.ResStrings.ContainsKey(VER_ICN[LABEL_ID]))
                        labels = info.ResStrings[VER_ICN[LABEL_ID]];
                    if (labels.Count > 0)
                    {
                        info.Label = labels[0];
                    }
                }
            }
        }
        catch (Exception e)
        {
            throw e;
        }
        return info;
    }

    private void ExtractPermission(ApkInfo info, XmlDocument doc, string keyName, string attribName)
    {
        XmlNodeList usesPermissions = doc.GetElementsByTagName(keyName);

        if (usesPermissions != null)
        {
            for (int s = 0; s < usesPermissions.Count; s++)
            {
                XmlNode permissionNode = usesPermissions.Item(s);
                if (permissionNode.NodeType == XmlNodeType.Element)
                {
                    XmlNode node = permissionNode.Attributes.GetNamedItem(attribName);
                    if (node != null)
                        info.Permissions.Add(node.Value);
                }
            }
        }
    }

    private string FindInDocument(XmlDocument doc, string keyName, string attribName)
    {
        XmlNodeList usesPermissions = doc.GetElementsByTagName(keyName);

        if (usesPermissions != null)
        {
            for (int s = 0; s < usesPermissions.Count; s++)
            {
                XmlNode permissionNode = usesPermissions.Item(s);
                if (permissionNode.NodeType == XmlNodeType.Element)
                {
                    XmlNode node = permissionNode.Attributes.GetNamedItem(attribName);
                    if (node != null)
                        return node.Value;
                }
            }
        }
        return null;
    }
}
