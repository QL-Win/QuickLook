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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

/// <summary>
/// Greenfish Icon Editor Pro native document (.gfie / .gfi).
/// Layers are ordered with index 0 = top, last = bottom (GFIE convention).
/// </summary>
internal sealed class GfieDocument
{
    public const int MaxWidth = 16384;
    public const int MaxHeight = 16384;

    public sealed class BitmapWithInvertedColor
    {
        public Bitmap Image;
        public Bitmap InversionMask;

        public BitmapWithInvertedColor()
        {
        }

        public BitmapWithInvertedColor(Bitmap bm)
        {
            Image = bm;
        }
    }

    public sealed class Metadata
    {
        public string Title = "";
        public string Author = "";
        public string Copyright = "";
        public string Comments = "";
        public int LoopCount;
        public double Dpi;

        public void Clear()
        {
            Title = "";
            Author = "";
            Copyright = "";
            Comments = "";
            LoopCount = 0;
            Dpi = 0;
        }
    }

    public enum SelectionState
    {
        None,
        Selecting,
        Floating
    }

    private static readonly string[] SelectionStateToString = ["none", "selecting", "floating"];

    public enum BlendMode
    {
        Normal, Mask, Behind, Dissolve,
        Hue, HueShift, Saturation,
        Darken, Multiply, ColorBurn, LinearBurn, DarkerColor,
        Lighten, Screen, ColorDodge, LinearDodge, LighterColor,
        Overlay, SoftLight, HardLight,
        VividLight, LinearLight, PinLight, HardMix,
        Difference, Exclusion
    }

    private static readonly string[] BlendModeToString =
    [
        "normal", "mask", "behind", "dissolve",
        "hue", "hueShift", "saturation",
        "darken", "multiply", "colorBurn", "linearBurn", "darkerColor",
        "lighten", "screen", "colorDodge", "linearDodge", "lighterColor",
        "overlay", "softLight", "hardLight",
        "vividLight", "linearLight", "pinLight", "hardMix",
        "difference", "exclusion"
    ];

    public sealed class SelectionInfo
    {
        public SelectionState State;
        public BitmapWithInvertedColor Image;
        public Bitmap Mask;
        public Rectangle Box;
        public double Angle;
        public int Depth;
    }

    public sealed class Layer
    {
        public string Name = "";
        public bool Visible = true;
        public bool Selected;
        public BitmapWithInvertedColor Image;
        public int Opacity = 255;
        public BlendMode BlendMode = BlendMode.Normal;
    }

    public sealed class LayerCollection
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public List<Layer> Layers { get; } = [];
        public SelectionInfo Selection { get; } = new();

        public void Resize(int w, int h)
        {
            Width = w;
            Height = h;
        }
    }

    public sealed class Page
    {
        public LayerCollection Layers { get; set; } = new();
        public Point HotSpot;
        public int FrameRate; // milliseconds
        public double Dpi;
    }

    public List<Page> Pages { get; } = [];
    public Metadata Data { get; } = new();

    public void Clear()
    {
        foreach (var page in Pages)
            DisposePage(page);
        Pages.Clear();
        Data.Clear();
    }

    public static bool IsGfieFile(Stream s) => GfTree.CanLoad(s);

    public static bool IsGfieFile(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return IsGfieFile(fs);
        }
        catch
        {
            return false;
        }
    }

    private static Point ReadPoint(GfTree t)
    {
        var result = new Point(0, 0);
        if (t.Descend("x")) { result.X = t.CurrentNode.AsInt; t.Ascend(); }
        if (t.Descend("y")) { result.Y = t.CurrentNode.AsInt; t.Ascend(); }
        return result;
    }

    private static Rectangle ReadRect(GfTree t)
    {
        int left = 0, top = 0, right = 0, bottom = 0;
        if (t.Descend("left")) { left = t.CurrentNode.AsInt; t.Ascend(); }
        if (t.Descend("top")) { top = t.CurrentNode.AsInt; t.Ascend(); }
        if (t.Descend("right")) { right = t.CurrentNode.AsInt; t.Ascend(); }
        if (t.Descend("bottom")) { bottom = t.CurrentNode.AsInt; t.Ascend(); }
        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private static Bitmap ReadRawImage(GfTree t)
    {
        using var s = new MemoryStream(t.CurrentNode.Data, writable: false);
        using var tmp = Image.FromStream(s, useEmbeddedColorManagement: false, validateImageData: false);
        return new Bitmap(tmp);
    }

    private static BitmapWithInvertedColor ReadBitmap(GfTree t)
    {
        if (!t.Descend("format"))
            return null;

        var fmt = t.CurrentNode.AsString.ToUpperInvariant();
        t.Ascend();

        if (fmt != "BMP" && fmt != "PNG")
            return null;

        if (!t.Descend("data"))
            return null;

        var result = new BitmapWithInvertedColor
        {
            Image = ReadRawImage(t)
        };
        t.Ascend();

        if (t.Descend("inversionMask"))
        {
            result.InversionMask = ReadRawImage(t);
            t.Ascend();
        }

        return result;
    }

    public bool Load(Stream s)
    {
        var t = new GfTree();
        if (!t.Load(s))
            return false;

        Clear();

        try
        {
            if (t.Descend("metadata"))
            {
                if (t.Descend("title")) { Data.Title = t.CurrentNode.AsString; t.Ascend(); }
                if (t.Descend("author")) { Data.Author = t.CurrentNode.AsString; t.Ascend(); }
                if (t.Descend("copyright")) { Data.Copyright = t.CurrentNode.AsString; t.Ascend(); }
                if (t.Descend("comments")) { Data.Comments = t.CurrentNode.AsString; t.Ascend(); }
                if (t.Descend("loopCount")) { Data.LoopCount = t.CurrentNode.AsInt; t.Ascend(); }
                if (t.Descend("dpi")) { Data.Dpi = t.CurrentNode.AsDouble; t.Ascend(); }
                t.Ascend();
            }

            if (t.Descend("pages"))
            {
                for (var i = 0; ; ++i)
                {
                    if (!t.Descend("page" + i))
                        break;

                    var pg = new Page();
                    Pages.Add(pg);

                    if (t.Descend("layers"))
                    {
                        var ls = pg.Layers;

                        if (t.Descend("size"))
                        {
                            var p = ReadPoint(t);
                            ls.Resize(Math.Min(MaxWidth, p.X), Math.Min(MaxHeight, p.Y));
                            t.Ascend();
                        }

                        for (var j = 0; ; ++j)
                        {
                            if (!t.Descend("layer" + j))
                                break;

                            var l = new Layer();
                            ls.Layers.Add(l);

                            if (t.Descend("name")) { l.Name = t.CurrentNode.AsString; t.Ascend(); }
                            if (t.Descend("visible")) { l.Visible = t.CurrentNode.AsBool; t.Ascend(); }
                            if (t.Descend("selected")) { l.Selected = t.CurrentNode.AsBool; t.Ascend(); }
                            if (t.Descend("image")) { l.Image = ReadBitmap(t); t.Ascend(); }
                            if (t.Descend("opacity")) { l.Opacity = t.CurrentNode.AsInt; t.Ascend(); }
                            if (t.Descend("blendMode"))
                            {
                                var idx = Array.IndexOf(BlendModeToString, t.CurrentNode.AsString);
                                l.BlendMode = idx >= 0 ? (BlendMode)idx : BlendMode.Normal;
                                t.Ascend();
                            }

                            t.Ascend();
                        }

                        if (t.Descend("selection"))
                        {
                            if (t.Descend("state"))
                            {
                                var idx = Array.IndexOf(SelectionStateToString, t.CurrentNode.AsString);
                                ls.Selection.State = idx >= 0 ? (SelectionState)idx : SelectionState.None;
                                t.Ascend();
                            }

                            switch (ls.Selection.State)
                            {
                                case SelectionState.Selecting:
                                    if (t.Descend("mask"))
                                    {
                                        ls.Selection.Mask = ReadBitmap(t)?.Image;
                                        t.Ascend();
                                    }
                                    break;

                                case SelectionState.Floating:
                                    if (t.Descend("image")) { ls.Selection.Image = ReadBitmap(t); t.Ascend(); }
                                    if (t.Descend("box")) { ls.Selection.Box = ReadRect(t); t.Ascend(); }
                                    if (t.Descend("angle")) { ls.Selection.Angle = t.CurrentNode.AsDouble; t.Ascend(); }
                                    if (t.Descend("depth")) { ls.Selection.Depth = t.CurrentNode.AsInt; t.Ascend(); }
                                    break;
                            }

                            t.Ascend();
                        }

                        t.Ascend();
                    }

                    if (t.Descend("hotSpot")) { pg.HotSpot = ReadPoint(t); t.Ascend(); }
                    if (t.Descend("frameRate")) { pg.FrameRate = t.CurrentNode.AsInt; t.Ascend(); }
                    if (t.Descend("dpi")) { pg.Dpi = t.CurrentNode.AsDouble; t.Ascend(); }

                    t.Ascend();
                }

                t.Ascend();
            }

            return true;
        }
        catch
        {
            Clear();
            return false;
        }
    }

    public bool Load(string filename)
    {
        using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Load(fs);
    }

    /// <summary>
    /// Lightweight size probe: reads page dimensions without decoding layer bitmaps.
    /// </summary>
    public static System.Windows.Size TryGetPreferredSize(string path, int preferredSize = 256)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var t = new GfTree();
            if (!t.Load(fs))
                return System.Windows.Size.Empty;

            var bestW = 0;
            var bestH = 0;

            if (!t.Descend("pages"))
                return System.Windows.Size.Empty;

            for (var i = 0; ; ++i)
            {
                if (!t.Descend("page" + i))
                    break;

                var w = 0;
                var h = 0;
                if (t.Descend("layers"))
                {
                    if (t.Descend("size"))
                    {
                        var p = ReadPoint(t);
                        w = Math.Min(MaxWidth, p.X);
                        h = Math.Min(MaxHeight, p.Y);
                        t.Ascend();
                    }
                    t.Ascend();
                }

                if (w > 0 && h > 0)
                {
                    var b1 = w >= preferredSize;
                    var b2 = w > bestW;
                    if (bestW == 0 || (b1 && (!b2 || bestW < preferredSize)) || (!b1 && b2))
                    {
                        bestW = w;
                        bestH = h;
                    }
                }

                t.Ascend();
            }

            return bestW > 0 && bestH > 0
                ? new System.Windows.Size(bestW, bestH)
                : System.Windows.Size.Empty;
        }
        catch
        {
            return System.Windows.Size.Empty;
        }
    }

    /// <summary>
    /// Pick the page best suited for preview (largest page, matching GFIE GetThumbnail).
    /// </summary>
    public Page GetBestPage(int preferredSize = 256)
    {
        if (Pages.Count == 0)
            return null;

        var best = Pages[0];
        for (var i = 1; i < Pages.Count; i++)
        {
            var pg = Pages[i];
            var b1 = pg.Layers.Width >= preferredSize;
            var b2 = pg.Layers.Width > best.Layers.Width;

            if ((b1 && (!b2 || best.Layers.Width < preferredSize)) || (!b1 && b2))
                best = pg;
        }

        return best;
    }

    public bool IsAnimated()
    {
        if (Pages.Count < 2)
            return false;

        for (var i = 0; i < Pages.Count; i++)
        {
            if (Pages[i].FrameRate > 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Flatten visible layers of a page into a 32-bpp ARGB bitmap.
    /// Index 0 is top; compositing walks bottom→top with SourceOver (GFIE Render order).
    /// Non-Normal blend modes fall back to Normal for preview.
    /// </summary>
    public static Bitmap Flatten(Page page)
    {
        if (page == null)
            return null;

        var ls = page.Layers;
        var width = Math.Max(1, ls.Width);
        var height = Math.Max(1, ls.Height);
        var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        using (var g = Graphics.FromImage(result))
        {
            g.Clear(Color.Transparent);

            // Bottom → top (last index → 0)
            for (var i = ls.Layers.Count - 1; i >= 0; i--)
            {
                var layer = ls.Layers[i];
                if (!layer.Visible || layer.BlendMode == BlendMode.Mask)
                    continue;

                if (layer.Image?.Image == null)
                    continue;

                DrawLayer(g, layer.Image.Image, layer.Opacity);

                // Floating selection drawn at its depth (as if it were that layer)
                if (ls.Selection.State == SelectionState.Floating &&
                    ls.Selection.Depth == i &&
                    ls.Selection.Image?.Image != null)
                {
                    var box = ls.Selection.Box;
                    if (box.Width > 0 && box.Height > 0)
                    {
                        var state = g.Save();
                        try
                        {
                            if (Math.Abs(ls.Selection.Angle) > 0.001)
                            {
                                g.TranslateTransform(box.X + box.Width / 2f, box.Y + box.Height / 2f);
                                g.RotateTransform((float)(ls.Selection.Angle * 180.0 / Math.PI));
                                g.TranslateTransform(-box.Width / 2f, -box.Height / 2f);
                                DrawLayer(g, ls.Selection.Image.Image, 255, new Rectangle(0, 0, box.Width, box.Height));
                            }
                            else
                            {
                                DrawLayer(g, ls.Selection.Image.Image, 255, box);
                            }
                        }
                        finally
                        {
                            g.Restore(state);
                        }
                    }
                }
            }
        }

        return result;
    }

    private static void DrawLayer(Graphics g, Bitmap image, int opacity, Rectangle? destRect = null)
    {
        if (opacity <= 0)
            return;

        var dest = destRect ?? new Rectangle(0, 0, image.Width, image.Height);

        if (opacity >= 255)
        {
            g.DrawImage(image, dest);
            return;
        }

        var colorMatrix = new ColorMatrix
        {
            Matrix33 = opacity / 255f
        };
        using var attrs = new ImageAttributes();
        attrs.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        g.DrawImage(image, dest, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attrs);
    }

    private static void DisposePage(Page page)
    {
        if (page?.Layers == null)
            return;

        foreach (var layer in page.Layers.Layers)
        {
            layer.Image?.Image?.Dispose();
            layer.Image?.InversionMask?.Dispose();
        }

        page.Layers.Selection.Image?.Image?.Dispose();
        page.Layers.Selection.Image?.InversionMask?.Dispose();
        page.Layers.Selection.Mask?.Dispose();
    }
}
