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

using QuickLook.Common.Plugin;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace QuickLook.Plugin.AppViewer;

public class Plugin : IViewer
{
    private static readonly string[] _extensions =
    [
        // Android
        //".apk", ".apk.1", // Android Package
        //".aar", // Android Archive
        //".aab", // Android App Bundle

        // Windows
        //".appx", ".appxbundle", // Windows APPX installer
        ".msi", // Windows MSI installer
        //".msix", ".msixbundle", // Windows MSIX installer

        // macOS
        //".dmg", // macOS DMG

        // iOS
        //".ipa", // iOS IPA

        // HarmonyOS
        //".hap", // HarmonyOS Package
        //".har", // HarmonyOS Archive

        // Others
        //".wgt", ".wgtu", // UniApp Widget
    ];

    private IAppInfoPanel _ip;
    private string _path;

    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return !Directory.Exists(path) && _extensions.Any(path.ToLower().EndsWith);
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = Path.GetExtension(path) switch
        {
            ".msi" => new Size { Width = 520, Height = 230 },
            _ => throw new NotSupportedException("Extension is not supported."),
        };
    }

    public void View(string path, ContextObject context)
    {
        _path = path;
        _ip = Path.GetExtension(path) switch
        {
            ".msi" => new MsiInfoPanel(),
            _ => throw new NotSupportedException("Extension is not supported."),
        };

        _ip.DisplayInfo(_path);
        _ip.Tag = context;

        context.ViewerContent = _ip;
        context.IsBusy = false;
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _ip = null;
    }
}
