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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickLook.Plugin.AppViewer.PackageParsers.Ipa;
using System.Collections.Generic;
using System.Text;

namespace QuickLook.Plugin.AppViewer.Tests;

[TestClass]
public class PlistTests
{
    [TestMethod]
    public void ReadPlist_IgnoresXmlComments()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
                <!-- The app name -->
                <key>CFBundleDisplayName</key>
                <string>Test App</string>
                <!-- Icon variants -->
                <key>CFBundleIconFiles</key>
                <array>
                    <!-- Legacy icon -->
                    <string>Icon-57.png</string>
                    <string>Icon-180.png</string>
                </array>
            </dict>
            </plist>
            """;

        var result = (Dictionary<string, object>)Plist.ReadPlist(Encoding.UTF8.GetBytes(xml));

        Assert.AreEqual("Test App", result["CFBundleDisplayName"]);
        var icons = (List<object>)result["CFBundleIconFiles"];
        CollectionAssert.AreEqual(new object[] { "Icon-57.png", "Icon-180.png" }, icons);
    }
}
