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

using Com.Opensource.Svga;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace QuickLook.Plugin.ImageViewer.Webview.Svga;

/// <summary>
/// Migrate from SVGAPlayer.Data.cs
/// https://github.com/svga/SVGAPlayer-UWP/blob/master/Svga/SvgaPlayer/Controls/SvgaPlayer.Data.cs
/// </summary>
public partial class SvgaPlayer
{
    /// <summary>
    /// Original binary data of the SVGA file
    /// </summary>
    private byte[] _inflatedBytes;

    /// <summary>
    /// SVGA configuration parameters.
    /// </summary>
    private MovieParams _movieParams;

    /// <summary>
    /// List of SVGA Sprite Entities
    /// </summary>
    private List<SpriteEntity> _sprites;

    /// <summary>
    /// Number of Sprites
    /// </summary>
    private int _spriteCount;

    public int SpriteCount
    {
        get => _spriteCount;
        set => _spriteCount = value;
    }

    /// <summary>
    /// Number of playback loops, default is 0
    /// When 0, it means infinite loop playback
    /// </summary>
    public int LoopCount { get; set; }

    /// <summary>
    /// Current playback frame
    /// </summary>
    private int _currentFrame;

    public int CurrentFrame
    {
        get => _currentFrame;
        private set => _currentFrame = value;
    }

    /// <summary>
    /// Whether it is in playing state
    /// </summary>
    private bool _isInPlay;

    public bool IsInPlay
    {
        get => _isInPlay;
        set => _isInPlay = value;
    }

    /// <summary>
    /// Total number of animation frames
    /// </summary>
    private int _totalFrame;

    public int TotalFrame
    {
        get => _totalFrame;
        private set => _totalFrame = value;
    }

    /// <summary>
    /// Target playback frame rate
    /// If not set or set to 0, the default frame rate is used. If set, the custom frame rate is used
    /// </summary>
    private int _fps;

    public int Fps
    {
        get => _fps;
        set
        {
            if (value < 0) { value = 0; }
            _fps = value;
        }
    }

    /// <summary>
    /// Canvas width
    /// </summary>
    private float _stageWidth;

    public float StageWidth
    {
        get => _stageWidth;
        set => _stageWidth = value;
    }

    /// <summary>
    /// Canvas height
    /// </summary>
    private float _stageHeight;

    public float StageHeight
    {
        get => _stageHeight;
        set => _stageHeight = value;
    }

    /// <summary>
    /// Inflate the SVGA file to get its original data
    /// The SVGA file has been deflated, so the first step is to inflate it
    /// </summary>
    private void InflateSvgaFile(Stream svgaFileBuffer)
    {
        byte[] inflatedBytes;

        // The built-in DeflateStream in Microsoft .NET does not recognize the first two bytes of the file header. For SVGA, these two bytes are 78 9C, which is the default compression indicator for Deflate
        // For more information, see https://stackoverflow.com/questions/17212964/net-zlib-inflate-with-net-4-5
        // For Zlib file header, see https://stackoverflow.com/questions/9050260/what-does-a-zlib-header-look-like
        svgaFileBuffer.Seek(2, SeekOrigin.Begin);

        using (var deflatedStream = new DeflateStream(svgaFileBuffer, CompressionMode.Decompress))
        {
            using var stream = new MemoryStream();
            deflatedStream.CopyTo(stream);
            inflatedBytes = stream.ToArray();
        }

        _inflatedBytes = inflatedBytes;
    }

    /// <summary>
    /// Get the SVGA MovieEntity from the inflated data
    /// </summary>
    /// <param name="inflatedBytes"></param>
    private void InitMovieEntity()
    {
        if (_inflatedBytes == null)
        {
            return;
        }

        var moveEntity = MovieEntity.Parser.ParseFrom(_inflatedBytes);
        _movieParams = moveEntity.Params;
        _sprites = [.. moveEntity.Sprites];
        TotalFrame = moveEntity.Params.Frames;
        SpriteCount = _sprites.Count;
        StageWidth = _movieParams.ViewBoxWidth;
        StageHeight = _movieParams.ViewBoxHeight;
    }

    /// <summary>
    /// Load SVGA file data
    /// </summary>
    /// <param name="svgaFileBuffer">SVGA file binary Stream</param>
    public void LoadSvgaFileData(Stream svgaFileBuffer)
    {
        InflateSvgaFile(svgaFileBuffer);
        InitMovieEntity();

        // Clear the inflated bytes after parsing to free memory
        _inflatedBytes = null;
    }
}
