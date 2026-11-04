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
using QuickLook.Common.Helpers;

namespace QuickLook.Tests;

[TestClass]
public sealed class PathHelperTests
{
    [TestMethod]
    [DataRow(@"\\server\share\file.png", ".png")]
    [DataRow(@"\\server.domain\share\folder\image.JPG", ".JPG")]
    [DataRow(@"\\10.1.2.3\share\file.pdf", ".pdf")]
    [DataRow(@"\\wsl$\Ubuntu\home\user\file.png", ".png")]
    [DataRow(@"\\wsl.localhost\Ubuntu\home\user\file.png", ".png")]
    [DataRow(@"\\server\share\folder", "")]
    [DataRow(@"\\wsl$\Ubuntu\home\user", "")]
    [DataRow(@"C:\local\folder\file.png", ".png")]
    [DataRow(@"Z:\mapped\folder\file.png", ".png")]
    [DataRow(@"C:\local\folder\file", "")]
    [DataRow("", "")]
    public void GetExtensionHandlesUncAndLocalPaths(string path, string expected)
    {
        Assert.AreEqual(expected, PathHelper.GetExtension(path));
    }

    [TestMethod]
    [DataRow(@"\\server\share\file.png" + "\n")]
    [DataRow(@"\\server\share\bad""name.png")]
    [DataRow(@"""\\server\share\file.png""")]
    public void GetExtensionDoesNotThrowOnMalformedUncPaths(string path)
    {
        Assert.AreEqual(".png", PathHelper.GetExtension(path));
    }

    [TestMethod]
    [DataRow(@"\\server\share\file.lnk", true)]
    [DataRow(@"\\server\share\file.LNK", true)]
    [DataRow(@"\\wsl$\Ubuntu\home\file.png", false)]
    [DataRow(@"C:\local\folder\file.lnk", true)]
    [DataRow(@"C:\local\folder\file.png", false)]
    [DataRow(@"\\wsl$\Ubuntu\home", false)]
    public void IsShortcutHandlesUncAndLocalPaths(string path, bool expected)
    {
        Assert.AreEqual(expected, PathHelper.IsShortcut(path));
    }

    [TestMethod]
    [DataRow(@"\\server\share\file.png", true)]
    [DataRow(@"\\wsl$\Ubuntu\home\file.png", true)]
    [DataRow(@"""\\server\share\file.png""", true)]
    [DataRow(@"C:\local\folder\file.png", false)]
    [DataRow(@"Z:\mapped\file.png", false)]
    [DataRow(@"\server\share\file.png", false)]
    [DataRow("", false)]
    public void IsUncPathHandlesUncAndLocalPaths(string path, bool expected)
    {
        Assert.AreEqual(expected, PathHelper.IsUncPath(path));
    }

    [TestMethod]
    [DataRow(@"\\server\share\folder\file.png", "file.png")]
    [DataRow(@"\\wsl$\Ubuntu\home\user\file.png", "file.png")]
    [DataRow(@"C:\local\folder\file.png", "file.png")]
    [DataRow(@"\\server\share\bad""name.png", "bad\"name.png")]
    public void GetFileNameHandlesUncAndMalformedPaths(string path, string expected)
    {
        Assert.AreEqual(expected, PathHelper.GetFileName(path));
    }

    [TestMethod]
    [DataRow(@"\\server\share\folder\file.png", @"\\server\share")]
    [DataRow(@"\\wsl$\Ubuntu\home\user\file.png", @"\\wsl$\Ubuntu")]
    [DataRow(@"C:\local\folder\file.png", @"C:\")]
    [DataRow(@"Z:\mapped\file.png", @"Z:\")]
    public void GetPathRootHandlesUncAndLocalPaths(string path, string expected)
    {
        Assert.AreEqual(expected, PathHelper.GetPathRoot(path));
    }
}
