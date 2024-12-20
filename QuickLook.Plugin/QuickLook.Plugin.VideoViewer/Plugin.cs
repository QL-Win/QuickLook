// Copyright © 2017 Paddy Xu
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

using MediaInfo;
using QuickLook.Common.Plugin;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace QuickLook.Plugin.VideoViewer;

public class Plugin : IViewer
{
    private static MediaInfoLib _mediaInfo;

    private ViewerPanel _vp;

    public int Priority => -3;

    static Plugin()
    {
        _mediaInfo = new MediaInfoLib(Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            Environment.Is64BitProcess ? "MediaInfo-x64\\" : "MediaInfo-x86\\"));
        _mediaInfo.Option("Cover_Data", "base64");
    }

    public void Init()
    {
        QLVRegistry.Register();
    }

    public bool CanHandle(string path)
    {
        if (!Directory.Exists(path))
        {
            try
            {
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
