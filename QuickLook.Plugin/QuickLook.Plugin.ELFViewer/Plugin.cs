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

using ELFSharp.ELF;
using ELFSharp.MachO;
using ELFSharp.UImage;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.ELFViewer.InfoPanels;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace QuickLook.Plugin.ELFViewer;

public sealed class Plugin : IViewer
{
    /// <summary>
    /// Magic number of ELF files
    /// 0x7F 'E' 'L' 'F'
    /// </summary>
    private static readonly byte[] _magic = [0x7F, 0x45, 0x4C, 0x46];

    private static readonly string[] _extensions =
    [
        ".axf", ".bin", ".elf", ".o", ".out", ".prx", ".puff", ".ko", ".mod", "so",
    ];

    private IInfoPanel _ip;
    private string _path;
    private FileEnum _type;

    public int Priority => 11;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        return DetectFormat(path) != FileEnum.None;
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 520, Height = 192 };
        context.Title = string.Empty;
        context.TitlebarOverlap = false;
        context.TitlebarBlurVisibility = false;
        context.TitlebarColourVisibility = false;
        context.FullWindowDragging = true;
    }

    public void View(string path, ContextObject context)
    {
        _path = path;
        _type = DetectFormat(path);
        _ip = _type switch
        {
            FileEnum.ELF => new ELFInfoPanel(),
            FileEnum.MachO => new MachOInfoPanel(),
            FileEnum.UImage => new UImageInfoPanel(),
            _ => throw new NotImplementedException(),
        };

        _ip.DisplayInfo(_path);

        context.ViewerContent = _ip;
        context.IsBusy = false;
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);

        _ip = null;
    }

    private static FileEnum DetectFormat(string path)
    {
        if (Directory.Exists(path))
            return FileEnum.None;

        var pathLower = Path.GetFileName(path).ToLower();
        var extension = Path.GetExtension(pathLower);

        // UImage
        if (pathLower.EndsWith(".uimage"))
        {
            return FileEnum.UImage;
        }
        else if (pathLower.Equals("uimage"))
        {
            if (UImageReader.TryLoad(path, out _) == UImageResult.OK)
                return FileEnum.UImage;
        }

        // Mach-O
        if (pathLower.EndsWith(".dylib"))
        {
            return FileEnum.MachO;
        }
        else if (extension == string.Empty)
        {
            if (MachOReader.TryLoad(path, out _) != MachOResult.NotMachO)
                return FileEnum.MachO;
        }

        // ELF
        if (_extensions.Any(pathLower.EndsWith) || extension == string.Empty)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(fs);

            if (br.BaseStream.Length < Consts.MinimalELFSize)
                return FileEnum.None;

            var magic = br.ReadBytes(4);
            for (var i = 0; i < 4; i++)
                if (magic[i] != _magic[i])
                    return FileEnum.None;

            return FileEnum.ELF;
        }

        return FileEnum.None;
    }

    private enum FileEnum
    {
        None,
        ELF,
        UImage,
        MachO,
    }
}
