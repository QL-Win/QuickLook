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

using QuickLook.Plugin.ImageViewer.Webview.Svg;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace QuickLook.Plugin.ImageViewer.Webview.Svga;

public class SvgaImagePanel(IWebMetaProvider metaWeb) : SvgImagePanel()
{
    private readonly IWebMetaProvider _metaWeb = metaWeb;

    public override void Preview(string path)
    {
        FallbackPath = Path.GetDirectoryName(path);

        ObjectForScripting ??= new ScriptHandler(path, _metaWeb);

        _homePage = _resources["/svga2html.html"];
        NavigateToUri(new Uri("file://quicklook/"));
    }
}

[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public sealed class ScriptHandler(string path, IWebMetaProvider metaWeb)
{
    public string Path { get; } = path;
    public IWebMetaProvider MetaWeb { get; } = metaWeb;

    public async Task<string> GetPath()
    {
        return await Task.FromResult(new Uri(Path).AbsolutePath);
    }

    public async Task<string> GetSize()
    {
        var size = MetaWeb.GetSize();

        return await Task.FromResult($"{{\"width\":{size.Width},\"height\":{size.Height}}}");
    }
}
