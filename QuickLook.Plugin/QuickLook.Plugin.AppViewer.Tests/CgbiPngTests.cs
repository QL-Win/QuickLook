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
using System.Drawing;
using System.IO;

namespace QuickLook.Plugin.AppViewer.Tests;

[TestClass]
public class CgbiPngTests
{
    [TestMethod]
    public void ConvertToStandardPng_RestoresRedAndBlueChannels()
    {
        byte[] cgbi = CgbiSample.Create(2, 2, 7, 139, 237, 255);

        Assert.IsTrue(CgbiPng.IsCgbi(cgbi));

        byte[] png = CgbiPng.ConvertToStandardPng(cgbi);

        Assert.IsFalse(CgbiPng.IsCgbi(png));
        using var stream = new MemoryStream(png);
        using var bitmap = new Bitmap(stream);
        Color color = bitmap.GetPixel(0, 0);
        Assert.AreEqual(237, color.R);
        Assert.AreEqual(139, color.G);
        Assert.AreEqual(7, color.B);
        Assert.AreEqual(255, color.A);
    }

    [TestMethod]
    public void ConvertToStandardPng_LeavesRegularPngUntouched()
    {
        byte[] png = System.Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        Assert.IsFalse(CgbiPng.IsCgbi(png));
        Assert.AreSame(png, CgbiPng.ConvertToStandardPng(png));
    }
}
