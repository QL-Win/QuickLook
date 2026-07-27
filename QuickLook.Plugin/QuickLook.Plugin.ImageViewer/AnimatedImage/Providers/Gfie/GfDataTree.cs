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
 
// Adapted from Greenfish Image Converter (zlib license)
// Copyright (c) 2016 B. Szalkai
// https://greenfishsoftware.github.io/greenfish-icon-editor-pro.html

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

/// <summary>
/// Binary tree container used by the Greenfish (.gfie/.gfi) native document format.
/// Signature: 'gfdt' (0x74646667).
/// </summary>
internal sealed class GfNode(GfNode parent)
{
    public byte[] Data { get; set; } = [];
    public List<GfNode> Children { get; } = [];
    public GfNode Parent { get; } = parent;
    public string Id { get; set; } = "";

    public bool AsBool => Data.Length >= 1 && Data[0] != 0;

    public int AsInt => Data.Length >= sizeof(int) ? BitConverter.ToInt32(Data, 0) : 0;

    public double AsDouble => Data.Length >= sizeof(double) ? BitConverter.ToDouble(Data, 0) : 0;

    public string AsString => Encoding.UTF8.GetString(Data);

    public void Clear()
    {
        Data = [];
        Children.Clear();
    }

    public GfNode NewChild()
    {
        var result = new GfNode(this);
        Children.Add(result);
        return result;
    }
}

internal sealed class GfTree
{
    public const int GF_DATA_TREE_SIG = 0x74646667; // 'gfdt'
    private const byte GFDT_BLOCK_BEGIN = 60; // <
    private const byte GFDT_BLOCK_END = 62; // >

    public GfNode Root { get; private set; }
    public GfNode CurrentNode { get; private set; }

    public GfTree()
    {
        Root = new GfNode(null) { Id = "\\" };
        CurrentNode = Root;
    }

    public void Clear()
    {
        Root.Clear();
        CurrentNode = Root;
    }

    public GfNode GetChildById(string id)
    {
        return CurrentNode.Children.FirstOrDefault(n => n.Id == id);
    }

    public bool Descend(string id)
    {
        var n = GetChildById(id);
        if (n != null)
            CurrentNode = n;
        return n != null;
    }

    public void NewChild(string id)
    {
        var n = GetChildById(id);
        if (n != null)
        {
            CurrentNode = n;
        }
        else
        {
            CurrentNode = CurrentNode.NewChild();
            CurrentNode.Id = id;
        }
    }

    public bool Ascend()
    {
        var ok = CurrentNode.Parent != null;
        if (ok)
            CurrentNode = CurrentNode.Parent;
        return ok;
    }

    public static bool CanLoad(Stream s)
    {
        if (s.Length - s.Position < 4)
            return false;

        var sig = new byte[4];
        _ = s.Read(sig, 0, sig.Length);
        s.Seek(-sig.Length, SeekOrigin.Current);
        return BitConverter.ToInt32(sig, 0) == GF_DATA_TREE_SIG;
    }

    public bool Load(Stream s)
    {
        using var br = new BinaryReader(s, Encoding.UTF8, leaveOpen: true);
        return Load(br);
    }

    public bool Load(BinaryReader br)
    {
        try
        {
            var sig = br.ReadInt32();
            if (sig != GF_DATA_TREE_SIG)
                return false;

            Clear();
            var rootWasRead = false;

            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                var blockType = br.ReadByte();
                var endReached = false;

                switch (blockType)
                {
                    case GFDT_BLOCK_BEGIN:
                        var idLength = br.ReadByte();
                        var id = Encoding.UTF8.GetString(br.ReadBytes(idLength));

                        if (rootWasRead)
                        {
                            NewChild(id);
                        }
                        else
                        {
                            CurrentNode.Id = id;
                            rootWasRead = true;
                        }

                        var dataSize = br.ReadInt32();
                        CurrentNode.Data = br.ReadBytes(dataSize);
                        break;

                    case GFDT_BLOCK_END:
                        if (!Ascend())
                            endReached = true;
                        break;

                    default:
                        return false;
                }

                if (endReached)
                    break;
            }

            return CurrentNode == Root;
        }
        catch
        {
            return false;
        }
    }
}
