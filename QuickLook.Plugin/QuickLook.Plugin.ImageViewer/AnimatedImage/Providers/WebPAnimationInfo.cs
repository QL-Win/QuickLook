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

using System;
using System.IO;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

internal enum WebPAnimationInspectionStatus
{
    NotAnimated,
    Animated,
    Invalid
}

internal sealed class WebPAnimationInfo
{
    public int Width { get; set; }

    public int Height { get; set; }

    public int FrameCount { get; set; }
}

/// <summary>
/// Reads only the WebP container metadata needed to decide whether an animation is safe to decode.
/// Image data is deliberately skipped so a malformed file cannot allocate frame-sized buffers here.
/// </summary>
internal static class WebPAnimationInspector
{
    private const byte AnimationFlag = 0x02;

    public static WebPAnimationInspectionStatus Inspect(
        string fileName,
        int maxFrameCount,
        out WebPAnimationInfo info,
        out string error)
    {
        info = new WebPAnimationInfo();
        error = null;

        try
        {
            using var stream = new FileStream(
                fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            if (stream.Length < 12)
                return WebPAnimationInspectionStatus.NotAnimated;

            var header = new byte[12];
            ReadExactly(stream, header, header.Length);

            if (!IsFourCc(header, 0, "RIFF") || !IsFourCc(header, 8, "WEBP"))
                return WebPAnimationInspectionStatus.NotAnimated;

            var riffSize = BitConverter.ToUInt32(header, 4);
            if (riffSize < 4)
                return Invalid(info, out error, "RIFF 容器长度无效");

            var riffEnd = 8L + riffSize;
            if (riffEnd > stream.Length)
                return Invalid(info, out error, "RIFF 容器被截断");

            var hasVp8x = false;
            var animationFlagSet = false;
            var hasAnimChunk = false;
            var position = 12L;
            var chunkHeader = new byte[16];

            while (position + 8 <= riffEnd)
            {
                stream.Position = position;
                ReadExactly(stream, chunkHeader, 8);

                var chunkSize = BitConverter.ToUInt32(chunkHeader, 4);
                var dataStart = position + 8;
                var nextPosition = dataStart + chunkSize + (chunkSize & 1);

                if (nextPosition < dataStart || nextPosition > riffEnd)
                    return Invalid(info, out error, "WebP 数据块长度无效");

                if (IsFourCc(chunkHeader, 0, "VP8X"))
                {
                    if (hasVp8x || chunkSize < 10)
                        return Invalid(info, out error, "VP8X 数据块无效");

                    hasVp8x = true;
                    stream.Position = dataStart;
                    ReadExactly(stream, chunkHeader, 10);

                    animationFlagSet = (chunkHeader[0] & AnimationFlag) != 0;
                    info.Width = ReadUInt24LittleEndian(chunkHeader, 4) + 1;
                    info.Height = ReadUInt24LittleEndian(chunkHeader, 7) + 1;
                }
                else if (IsFourCc(chunkHeader, 0, "ANIM"))
                {
                    if (chunkSize < 6)
                        return Invalid(info, out error, "ANIM 数据块无效");

                    hasAnimChunk = true;
                }
                else if (IsFourCc(chunkHeader, 0, "ANMF"))
                {
                    if (chunkSize < 16 || !hasVp8x || info.Width <= 0 || info.Height <= 0)
                        return Invalid(info, out error, "ANMF 数据块缺少有效的画布信息");

                    if (info.FrameCount >= maxFrameCount)
                        return Invalid(info, out error, $"动画帧数超过上限 {maxFrameCount}");

                    stream.Position = dataStart;
                    ReadExactly(stream, chunkHeader, 12);

                    var frameX = ReadUInt24LittleEndian(chunkHeader, 0) * 2L;
                    var frameY = ReadUInt24LittleEndian(chunkHeader, 3) * 2L;
                    var frameWidth = ReadUInt24LittleEndian(chunkHeader, 6) + 1L;
                    var frameHeight = ReadUInt24LittleEndian(chunkHeader, 9) + 1L;

                    if (frameX + frameWidth > info.Width || frameY + frameHeight > info.Height)
                        return Invalid(info, out error, "动画帧超出 WebP 画布范围");

                    info.FrameCount++;
                }

                position = nextPosition;
            }

            if (!hasVp8x)
            {
                return info.FrameCount > 0
                    ? Invalid(info, out error, "动画 WebP 缺少 VP8X 数据块")
                    : WebPAnimationInspectionStatus.NotAnimated;
            }

            if (!animationFlagSet)
            {
                return info.FrameCount > 0
                    ? Invalid(info, out error, "WebP 动画标志与 ANMF 数据块不一致")
                    : WebPAnimationInspectionStatus.NotAnimated;
            }

            if (!hasAnimChunk || info.FrameCount < 2)
                return Invalid(info, out error, "动画 WebP 的帧信息不完整");

            return WebPAnimationInspectionStatus.Animated;
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
        {
            return Invalid(info, out error, e.Message);
        }
    }

    private static WebPAnimationInspectionStatus Invalid(
        WebPAnimationInfo info,
        out string error,
        string message)
    {
        error = message;
        return WebPAnimationInspectionStatus.Invalid;
    }

    private static void ReadExactly(Stream stream, byte[] buffer, int count)
    {
        var offset = 0;

        while (offset < count)
        {
            var read = stream.Read(buffer, offset, count - offset);
            if (read == 0)
                throw new EndOfStreamException("WebP 数据意外结束");

            offset += read;
        }
    }

    private static bool IsFourCc(byte[] buffer, int offset, string value)
    {
        return buffer[offset] == value[0]
            && buffer[offset + 1] == value[1]
            && buffer[offset + 2] == value[2]
            && buffer[offset + 3] == value[3];
    }

    private static int ReadUInt24LittleEndian(byte[] buffer, int offset)
    {
        return buffer[offset]
            | (buffer[offset + 1] << 8)
            | (buffer[offset + 2] << 16);
    }
}
