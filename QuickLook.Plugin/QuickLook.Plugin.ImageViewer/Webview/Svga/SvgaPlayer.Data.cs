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

using Com.Opensource.Svga;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

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
    /// Whether the data is in JSON format (SVGA 1.x) or protobuf format (SVGA 2.x)
    /// </summary>
    private bool _isJsonFormat;

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
    /// Check if the stream is a ZIP archive (SVGA 1.x format)
    /// ZIP files start with PK header (0x50, 0x4B)
    /// </summary>
    private static bool IsZipArchive(Stream stream)
    {
        var originalPosition = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);

        var header = new byte[2];
        var bytesRead = stream.Read(header, 0, 2);

        stream.Seek(originalPosition, SeekOrigin.Begin);

        return bytesRead == 2 && header[0] == 0x50 && header[1] == 0x4B; // PK
    }

    /// <summary>
    /// Check if the data is JSON format (SVGA 1.x)
    /// Handles UTF-8 BOM and leading whitespace
    /// </summary>
    private static bool IsJsonPayload(byte[] data)
    {
        int i = 0;
        
        // Skip UTF-8 BOM (EF BB BF)
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
        {
            i = 3;
        }
        
        // Skip leading whitespace
        while (i < data.Length && (data[i] == ' ' || data[i] == '\t' || data[i] == '\r' || data[i] == '\n'))
        {
            i++;
        }
        
        // Check if first non-whitespace byte is '{'
        return i < data.Length && data[i] == '{';
    }

    /// <summary>
    /// Extract SVGA data from ZIP archive (SVGA 1.x format)
    /// SVGA 1.x stores JSON data in "movie.spec" file
    /// </summary>
    private (byte[] data, bool isJson) ExtractFromZip(Stream svgaFileBuffer)
    {
        svgaFileBuffer.Seek(0, SeekOrigin.Begin);

        using var archive = new ZipArchive(svgaFileBuffer, ZipArchiveMode.Read, leaveOpen: true);
        
        // SVGA 1.x stores the JSON data in "movie.spec"
        foreach (var entry in archive.Entries)
        {
            if (entry.Name.Equals("movie.spec", StringComparison.OrdinalIgnoreCase))
            {
                using var entryStream = entry.Open();
                using var memoryStream = new MemoryStream();
                entryStream.CopyTo(memoryStream);
                var data = memoryStream.ToArray();
                
                // Check if it's JSON or protobuf
                // Skip UTF-8 BOM (EF BB BF) and leading whitespace
                bool isJson = IsJsonPayload(data);
                return (data, isJson);
            }
        }

        // Fallback: try to find any .spec file
        foreach (var entry in archive.Entries)
        {
            if (entry.Name.EndsWith(".spec", StringComparison.OrdinalIgnoreCase))
            {
                using var entryStream = entry.Open();
                using var memoryStream = new MemoryStream();
                entryStream.CopyTo(memoryStream);
                var data = memoryStream.ToArray();
                bool isJson = IsJsonPayload(data);
                return (data, isJson);
            }
        }

        throw new InvalidDataException("No valid SVGA data found in ZIP archive");
    }

    /// <summary>
    /// Inflate the SVGA file to get its original data
    /// Supports both SVGA 1.x (ZIP/JSON) and 2.x (zlib/protobuf) formats
    /// </summary>
    private void InflateSvgaFile(Stream svgaFileBuffer)
    {
        if (IsZipArchive(svgaFileBuffer))
        {
            // SVGA 1.x format: ZIP archive containing JSON data
            var (data, isJson) = ExtractFromZip(svgaFileBuffer);
            _inflatedBytes = data;
            _isJsonFormat = isJson;
        }
        else
        {
            // SVGA 2.x format: zlib compressed protobuf data
            // The built-in DeflateStream in Microsoft .NET does not recognize the first two bytes of the file header. For SVGA, these two bytes are 78 9C, which is the default compression indicator for Deflate
            // For more information, see https://stackoverflow.com/questions/17212964/net-zlib-inflate-with-net-4-5
            // For Zlib file header, see https://stackoverflow.com/questions/9050260/what-does-a-zlib-header-look-like
            svgaFileBuffer.Seek(2, SeekOrigin.Begin);

            using (var deflatedStream = new DeflateStream(svgaFileBuffer, CompressionMode.Decompress))
            {
                using var stream = new MemoryStream();
                deflatedStream.CopyTo(stream);
                _inflatedBytes = stream.ToArray();
            }
            _isJsonFormat = false;
        }
    }

    /// <summary>
    /// Get the SVGA MovieEntity from the inflated data
    /// Supports both JSON (SVGA 1.x) and protobuf (SVGA 2.x) formats
    /// </summary>
    private void InitMovieEntity()
    {
        if (_inflatedBytes == null)
        {
            return;
        }

        if (_isJsonFormat)
        {
            // SVGA 1.x: JSON format
            InitMovieEntityFromJson();
        }
        else
        {
            // SVGA 2.x: Protobuf format
            var moveEntity = MovieEntity.Parser.ParseFrom(_inflatedBytes);
            _movieParams = moveEntity.Params;
            _sprites = [.. moveEntity.Sprites];
            TotalFrame = moveEntity.Params.Frames;
            SpriteCount = _sprites.Count;
            StageWidth = _movieParams.ViewBoxWidth;
            StageHeight = _movieParams.ViewBoxHeight;
        }
    }

    /// <summary>
    /// Parse SVGA 1.x JSON format
    /// </summary>
    private void InitMovieEntityFromJson()
    {
        var json = System.Text.Encoding.UTF8.GetString(_inflatedBytes);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("movie", out var movie))
        {
            if (movie.TryGetProperty("viewBox", out var viewBox))
            {
                StageWidth = viewBox.GetProperty("width").GetSingle();
                StageHeight = viewBox.GetProperty("height").GetSingle();
            }

            if (movie.TryGetProperty("frames", out var frames))
            {
                TotalFrame = frames.GetInt32();
            }

            if (movie.TryGetProperty("fps", out var fps))
            {
                Fps = fps.GetInt32();
            }
        }

        if (root.TryGetProperty("sprites", out var sprites))
        {
            SpriteCount = sprites.GetArrayLength();
        }

        // Create a minimal MovieParams for compatibility
        _movieParams = new MovieParams
        {
            ViewBoxWidth = StageWidth,
            ViewBoxHeight = StageHeight,
            Frames = TotalFrame,
            Fps = Fps
        };
        _sprites = new List<SpriteEntity>();
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
