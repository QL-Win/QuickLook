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

using QuickLook.Common.Plugin;
using QuickLook.Plugin.ThumbnailViewer.Providors;
using System.IO;

namespace QuickLook.Plugin.ThumbnailViewer;

internal static class Handler
{
    public static void Prepare(string path, ContextObject context)
    {
        (Path.GetExtension(path).ToLower() switch
        {
            ".cdr" => new CdrProvider(),
            ".fig" => new FigProvidor(),
            ".kra" => new KraProvidor(),
            ".pdn" => new PdnProvider(),
            ".pip" or ".pix" => new PixProvidor(),
            ".sketch" => new SketchProvidor(),
            ".xd" => new XdProvidor(),
            ".xmind" => new XmindProvidor(),
            _ => (AbstractProvidor)null,
        })?.Prepare(path, context);
    }

    public static Stream ViewImage(string path)
    {
        return (Path.GetExtension(path).ToLower() switch
        {
            ".cdr" => new CdrProvider(),
            ".fig" => new FigProvidor(),
            ".kra" => new KraProvidor(),
            ".pdn" => new PdnProvider(),
            ".pip" or ".pix" => new PixProvidor(),
            ".sketch" => new SketchProvidor(),
            ".xd" => new XdProvidor(),
            ".xmind" => new XmindProvidor(),
            _ => (AbstractProvidor)null,
        })?.ViewImage(path);
    }
}
