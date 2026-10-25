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
using QuickLook.MediaInfo;
using QuickLook.MediaInfo.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace QuickLook.Plugin.VideoViewer;

public sealed class Plugin : IViewer
{
    /// <summary>
    /// Extensions opened directly with a full MediaInfo parse. Anything else is probed first,
    /// so media with unusual extensions still works while arbitrary binaries stay cheap.
    /// </summary>
    private static readonly HashSet<string> WellKnownExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".3g2", ".3gp", ".asf", ".avi", ".flv", ".m2ts", ".m4v", ".mkv", ".mov", ".mp4", ".mpeg", ".mpg",
        ".mts", ".ogv", ".rm", ".rmvb", ".ts", ".vob", ".webm", ".wmv",
        ".aac", ".ac3", ".aif", ".aiff", ".amr", ".ape", ".dts", ".flac", ".m4a", ".mka", ".mp3", ".oga",
        ".ogg", ".opus", ".wav", ".wma", ".wv",
    };

    /// <summary>Upper bound of bytes read when probing a file with an unknown extension.</summary>
    private const long MaxProbeBytes = 4 * 1024 * 1024;

    private static readonly MediaInfoNative _mediaInfo;

    /// <summary>
    /// Separate handle for <see cref="ProbeIsMedia"/>: calling Open() on a handle previously
    /// used in buffer mode crashes MediaInfo with an access violation.
    /// </summary>
    private static readonly MediaInfoNative _probeMediaInfo;

    private ViewerPanel _vp;

    public int Priority => -3;

    static Plugin()
    {
        var nativePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            Environment.Is64BitProcess ? @"runtimes\win-x64\native\" : @"runtimes\win-x86\native\");

        _mediaInfo = new(nativePath);
        _mediaInfo.Option("Cover_Data", "base64");
        // Only header fields (format, size, rotation, tags, cover) are used. The default parse
        // speed also samples the stream, reading tens of MB per file and stalling on slow disks.
        _mediaInfo.Option("ParseSpeed", "0");

        _probeMediaInfo = new(nativePath);
        _probeMediaInfo.Option("ParseSpeed", "0");
    }

    public void Init()
    {
        // Remove legacy LAV hardware acceleration settings
        // https://github.com/QL-Win/QuickLook/issues/1928
#if false
        QLVRegistry.Register();
#endif
    }

    public bool CanHandle(string path)
    {
        if (!Directory.Exists(path))
        {
            try
            {
                // MediaInfo.Open() on an arbitrary binary can read the whole file several times
                // (sync-word scanning for headerless formats like MPEG-TS or raw MP3), which freezes
                // the UI on large files. Probe unknown extensions with a bounded read first.
                if (!WellKnownExtensions.Contains(Path.GetExtension(path)) && !ProbeIsMedia(path))
                    return false;

                _mediaInfo.Open(path);
                string videoCodec = _mediaInfo.Get(StreamKind.Video, 0, "Format");
                string audioCodec = _mediaInfo.Get(StreamKind.Audio, 0, "Format");
                // Note MediaInfo.Close seems to close the dll and you have to re-create the MediaInfo
                //      object like in the static class constructor above. Any call to Get methods etc.
                //      will result in a "Unable to load MediaInfo library" error.
                // Ref: https://github.com/MediaArea/MediaInfoLib/blob/master/Source/MediaInfoDLL/MediaInfoDLL.cs
                // Pretty sure it doesn't leak when opening another file as the c++ code calls Close on Open
                // Ref: https://github.com/MediaArea/MediaInfoLib/blob/master/Source/MediaInfo/MediaInfo_Internal.cpp
                if (videoCodec == "Unable to load MediaInfo library") // should not happen
                {
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(videoCodec) || !string.IsNullOrWhiteSpace(audioCodec))
                {
                    return true;
                }
            }
            catch
            {
                // return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Feeds MediaInfo through its buffer API, honoring its seek requests but reading at most
    /// <see cref="MaxProbeBytes"/>, so non-media files are rejected without scanning them entirely.
    /// </summary>
    private static bool ProbeIsMedia(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var buffer = new byte[64 * 1024];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            long total = 0;
            _probeMediaInfo.OpenBufferInit(fs.Length, 0);
            while (total < MaxProbeBytes)
            {
                int read = fs.Read(buffer, 0, (int)Math.Min(buffer.Length, MaxProbeBytes - total));
                if (read <= 0)
                    break;
                total += read;

                // Bit 3 (0x08): MediaInfo has finished parsing
                if ((_probeMediaInfo.OpenBufferContinue(handle.AddrOfPinnedObject(), (IntPtr)read) & 0x08) != 0)
                    break;

                long goTo = _probeMediaInfo.OpenBufferContinueGoToGet();
                if (goTo != -1)
                {
                    fs.Seek(goTo, SeekOrigin.Begin);
                    _probeMediaInfo.OpenBufferInit(fs.Length, fs.Position);
                }
            }
            _probeMediaInfo.OpenBufferFinalize();
        }
        finally
        {
            handle.Free();
        }

        return !string.IsNullOrWhiteSpace(_probeMediaInfo.Get(StreamKind.Video, 0, "Format"))
            || !string.IsNullOrWhiteSpace(_probeMediaInfo.Get(StreamKind.Audio, 0, "Format"));
    }

    public void Prepare(string path, ContextObject context)
    {
        string videoCodec = _mediaInfo.Get(StreamKind.Video, 0, "Format");
        if (!string.IsNullOrWhiteSpace(videoCodec)) // video
        {
            int.TryParse(_mediaInfo.Get(StreamKind.Video, 0, "Width"), out var width);
            int.TryParse(_mediaInfo.Get(StreamKind.Video, 0, "Height"), out var height);
            double.TryParse(_mediaInfo.Get(StreamKind.Video, 0, "Rotation"), out var rotation);

            // Correct rotation: on some machine the value "90" becomes "90000" by some reason
            if (rotation > 360)
                rotation /= 1e3;

            var windowSize = new Size
            {
                Width = Math.Max(100, width == 0 ? 1366 : width),
                Height = Math.Max(100, height == 0 ? 768 : height)
            };

            if (rotation % 180 != 0)
                windowSize = new Size(windowSize.Height, windowSize.Width);

            context.SetPreferredSizeFit(windowSize, 0.8);

            context.TitlebarAutoHide = true;
            context.Theme = Themes.Dark;
            context.TitlebarBlurVisibility = true;
        }
        else // audio
        {
            context.PreferredSize = new Size(500, 300);

            context.CanResize = false;
            context.TitlebarAutoHide = false;
            context.TitlebarBlurVisibility = false;
            context.TitlebarColourVisibility = false;
        }

        context.TitlebarOverlap = true;
    }

    public void View(string path, ContextObject context)
    {
        _vp = new ViewerPanel(context);

        context.ViewerContent = _vp;

        context.Title = $"{Path.GetFileName(path)}";

        _vp.LoadAndPlay(path, _mediaInfo);
    }

    public void Cleanup()
    {
        _vp?.Dispose();
        _vp = null;
    }
}
