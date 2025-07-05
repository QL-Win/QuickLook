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

using System.IO;
using System.Windows;

namespace QuickLook.Plugin.ImageViewer.Webview.Svga;

internal class SvgaMetaProvider(string path) : IWebMetaProvider
{
    private readonly string _path = path;
    private Size _size = Size.Empty;

    public Size GetSize()
    {
        if (_size != Size.Empty)
        {
            return _size;
        }

        if (!File.Exists(_path))
        {
            return _size;
        }

        try
        {
            var svga = new SvgaPlayer();
            var fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);

            svga.LoadSvgaFileData(fileStream);
            return new Size(svga.StageWidth, svga.StageHeight);
        }
        catch
        {
            // That's fine, just return the default size.
        }

        return new Size(800, 600);
    }
}
