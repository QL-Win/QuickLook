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

using ICSharpCode.SharpZipLib.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickLook.Plugin.AppViewer.PackageParsers.Ipa;
using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace QuickLook.Plugin.AppViewer.Tests;

[TestClass]
public class IpaReaderTests
{
    private const string InfoPlist = """
        <?xml version="1.0" encoding="UTF-8"?>
        <plist version="1.0">
        <dict>
            <!-- Display name -->
            <key>CFBundleDisplayName</key>
            <string>Sample</string>
            <key>CFBundleShortVersionString</key>
            <string>1.2.3</string>
            <key>CFBundleVersion</key>
            <string>123</string>
            <key>CFBundleIdentifier</key>
            <string>com.example.sample</string>
            <key>MinimumOSVersion</key>
            <string>6.1</string>
            <key>UIDeviceFamily</key>
            <array>
                <integer>1</integer>
                <integer>2</integer>
            </array>
            <!-- Primary icon names -->
            <key>CFBundleIconFiles</key>
            <array>
                <string>AppIcon57x57</string>
                <string>AppIcon60x60</string>
            </array>
        </dict>
        </plist>
        """;

    [TestMethod]
    public void Parse_ReadsInfoPlistWithCommentsAndConvertsCgbiIcon()
    {
        byte[] cgbi = CgbiSample.Create(2, 2, 7, 139, 237, 255);

        using var stream = new MemoryStream();
        using (var zip = new ZipOutputStream(stream)
        {
            IsStreamOwner = false,
        })
        {
            AddEntry(zip, "Payload/Sample.app/Info.plist", Encoding.UTF8.GetBytes(InfoPlist));
            AddEntry(zip, "Payload/Sample.app/AppIcon60x60@2x.png", cgbi);
        }

        stream.Position = 0;
        IpaReader reader = new(stream);

        Assert.AreEqual("Sample", reader.DisplayName);
        Assert.AreEqual("1.2.3", reader.ShortVersionString);
        Assert.AreEqual("123", reader.Version);
        Assert.AreEqual("com.example.sample", reader.Identifier);
        Assert.AreEqual("iPhone, iPad", reader.DeviceFamily);
        Assert.IsNotNull(reader.Icon);

        using var iconStream = new MemoryStream(reader.Icon);
        using var bitmap = new Bitmap(iconStream);
        Color color = bitmap.GetPixel(0, 0);
        Assert.AreEqual(237, color.R);
        Assert.AreEqual(139, color.G);
        Assert.AreEqual(7, color.B);
    }

    private static void AddEntry(ZipOutputStream zip, string name, byte[] data)
    {
        ZipEntry entry = new(name)
        {
            Size = data.Length,
            DateTime = new DateTime(2020, 1, 1),
        };

        zip.PutNextEntry(entry);
        zip.Write(data, 0, data.Length);
        zip.CloseEntry();
    }
}
