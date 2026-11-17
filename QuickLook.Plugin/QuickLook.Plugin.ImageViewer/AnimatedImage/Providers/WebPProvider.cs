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

using ImageGlass.Base.Photoing.Codecs;
using ImageGlass.WebP;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

internal class WebPProvider : ImageMagickProvider
{
    private const int MaxAnimationFrameCount = 1000;
    private const int MaxCanvasDimension = 16384;
    private const int BytesPerPixel = 4;
    private const long MaxSourceFileBytes = 256L * 1024 * 1024;
    private const long MaxDecodedAnimationBytes = 512L * 1024 * 1024;
    private const long MaxStaticFrameBytes = 128L * 1024 * 1024;

    private readonly object _playbackLock = new();
    private CancellationTokenSource _playbackCancellation;
    private bool _disposed;

    public WebPProvider(Uri path, MetaProvider meta, ContextObject contextObject) : base(path, meta, contextObject)
    {
    }

    public override Task<BitmapSource> GetRenderedFrame(int index)
    {
        return new Task<BitmapSource>(() =>
        {
            WebPAnimationInfo animationInfo = null;

            try
            {
                // Unfortunately we only support Animated WebP on x64 platforms
                if (Environment.Is64BitProcess)
                {
                    var status = WebPAnimationInspector.Inspect(
                        Path.LocalPath,
                        MaxAnimationFrameCount,
                        out animationInfo,
                        out var error);

                    if (status == WebPAnimationInspectionStatus.Invalid)
                    {
                        return GetStaticFallback(
                            animationInfo,
                            $"Animated WebP rejected before decoding: {error}");
                    }

                    if (status == WebPAnimationInspectionStatus.Animated)
                    {
                        var fileLength = new FileInfo(Path.LocalPath).Length;
                        if (fileLength > MaxSourceFileBytes)
                        {
                            return GetStaticFallback(
                                animationInfo,
                                $"Animated WebP source is too large ({fileLength} bytes)");
                        }

                        if (!TryValidateAnimation(animationInfo, out error))
                        {
                            return GetStaticFallback(
                                animationInfo,
                                $"Animated WebP exceeds decode limits: {error}");
                        }

                        return AnimatedWebP(Path.LocalPath, animationInfo);
                    }
                }

                return base.GetRenderedFrame();
            }
            catch (OutOfMemoryException e)
            {
                StopPlayback();
                return GetStaticFallback(
                    animationInfo,
                    $"Out of memory while decoding WebP: {e.Message}");
            }
            catch (Exception e)
            {
                StopPlayback();
                return GetStaticFallback(
                    animationInfo,
                    $"Failed to decode animated WebP: {e}");
            }
        });
    }

    public override void Dispose()
    {
        CancellationTokenSource playbackCancellation;

        lock (_playbackLock)
        {
            if (_disposed)
                return;

            _disposed = true;
            playbackCancellation = _playbackCancellation;
            _playbackCancellation = null;
        }

        playbackCancellation?.Cancel();
        base.Dispose();
    }

    private BitmapSource AnimatedWebP(string fileName, WebPAnimationInfo animationInfo)
    {
        using var webP = new WebPWrapper();
        var frames = DecodeAnimationFrames(webP, fileName, animationInfo);
        AnimatedImg animatedImg = null;

        try
        {
            animatedImg = new AnimatedImg(frames, frames.Count);

            var writeableBitmap = Application.Current.Dispatcher.Invoke(() =>
            {
                var frame = animatedImg.Frames.GetEnumerator();
                try
                {
                    if (!frame.MoveNext() || frame.Current?.Bitmap is not Bitmap bitmap)
                        throw new InvalidDataException("Animated WebP contains no usable frames");

                    return bitmap.ToWriteableBitmap();
                }
                finally
                {
                    frame.Dispose();
                }
            });

            if (StartPlayback(animatedImg, writeableBitmap))
                animatedImg = null;

            return writeableBitmap;
        }
        catch
        {
            if (animatedImg != null)
                DisposeAnimatedImage(animatedImg);
            else
                DisposeFrames(frames);

            throw;
        }
    }

    private static List<AnimatedImgFrame> DecodeAnimationFrames(
        WebPWrapper webP,
        string fileName,
        WebPAnimationInfo animationInfo)
    {
        var frames = new List<AnimatedImgFrame>(animationInfo.FrameCount);

        try
        {
            foreach (var frameData in webP.AnimLoad(fileName))
            {
                var bitmap = frameData?.Bitmap;
                if (bitmap == null)
                    throw new InvalidDataException("Animated WebP returned an empty frame");

                if (bitmap.Width != animationInfo.Width || bitmap.Height != animationInfo.Height)
                {
                    bitmap.Dispose();
                    throw new InvalidDataException(
                        $"Animated WebP frame size {bitmap.Width}x{bitmap.Height} does not match " +
                        $"the declared canvas {animationInfo.Width}x{animationInfo.Height}");
                }

                var duration = frameData.Duration > 0
                    ? Math.Min(frameData.Duration, 60_000)
                    : 100;

                frames.Add(new AnimatedImgFrame(bitmap, (uint)duration));
            }

            if (frames.Count != animationInfo.FrameCount)
            {
                throw new InvalidDataException(
                    $"Animated WebP decoded {frames.Count} frames, expected {animationInfo.FrameCount}");
            }

            return frames;
        }
        catch
        {
            DisposeFrames(frames);
            throw;
        }
    }

    private bool StartPlayback(AnimatedImg animatedImg, WriteableBitmap writeableBitmap)
    {
        var cancellation = new CancellationTokenSource();
        CancellationTokenSource previousCancellation;

        lock (_playbackLock)
        {
            if (_disposed)
            {
                cancellation.Dispose();
                DisposeAnimatedImage(animatedImg);
                return false;
            }

            previousCancellation = _playbackCancellation;
            _playbackCancellation = cancellation;
        }

        previousCancellation?.Cancel();

        try
        {
            _ = Task.Factory.StartNew(
                () => PlayAnimation(animatedImg, writeableBitmap, cancellation),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            return true;
        }
        catch (Exception e)
        {
            lock (_playbackLock)
            {
                if (ReferenceEquals(_playbackCancellation, cancellation))
                    _playbackCancellation = null;
            }

            cancellation.Dispose();
            DisposeAnimatedImage(animatedImg);
            ProcessHelper.WriteLog($"Unable to start animated WebP playback: {e}");
            return false;
        }
    }

    private void PlayAnimation(
        AnimatedImg animatedImg,
        WriteableBitmap writeableBitmap,
        CancellationTokenSource cancellation)
    {
        var token = cancellation.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var frame in animatedImg.Frames)
                {
                    token.ThrowIfCancellationRequested();

                    if (frame?.Bitmap is not Bitmap bitmap)
                        throw new InvalidDataException("Animated WebP playback encountered an empty frame");

                    writeableBitmap.Dispatcher.Invoke(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        bitmap.CopyToWriteableBitmap(writeableBitmap);
                    }, DispatcherPriority.Normal, token);

                    var delay = frame.Duration <= TimeSpan.Zero
                        ? 100
                        : Math.Min((int)frame.Duration.TotalMilliseconds, 60_000);

                    if (token.WaitHandle.WaitOne(delay))
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog($"Animated WebP playback stopped: {e}");
        }
        finally
        {
            DisposeAnimatedImage(animatedImg);
            cancellation.Dispose();

            lock (_playbackLock)
            {
                if (ReferenceEquals(_playbackCancellation, cancellation))
                    _playbackCancellation = null;
            }
        }
    }

    private void StopPlayback()
    {
        CancellationTokenSource playbackCancellation;

        lock (_playbackLock)
        {
            playbackCancellation = _playbackCancellation;
            _playbackCancellation = null;
        }

        playbackCancellation?.Cancel();
    }

    private BitmapSource GetStaticFallback(WebPAnimationInfo animationInfo, string message)
    {
        ProcessHelper.WriteLog(message);

        if (!CanDecodeStaticFrame(animationInfo))
            return null!;

        try
        {
            return base.GetRenderedFrame();
        }
        catch (OutOfMemoryException e)
        {
            ProcessHelper.WriteLog($"Unable to render static WebP fallback: {e.Message}");
            return null!;
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog($"Unable to render static WebP fallback: {e}");
            return null!;
        }
    }

    private static bool TryValidateAnimation(WebPAnimationInfo info, out string error)
    {
        if (info.Width <= 0 || info.Height <= 0)
        {
            error = "canvas dimensions are invalid";
            return false;
        }

        if (info.Width > MaxCanvasDimension || info.Height > MaxCanvasDimension)
        {
            error = $"canvas {info.Width}x{info.Height} exceeds {MaxCanvasDimension}px";
            return false;
        }

        var frameBytes = (long)info.Width * info.Height * BytesPerPixel;
        var requiredBytesWithOutputBuffer = frameBytes * (info.FrameCount + 1L);

        if (requiredBytesWithOutputBuffer > MaxDecodedAnimationBytes)
        {
            error =
                $"{info.FrameCount} frames at {info.Width}x{info.Height} would require " +
                $"{requiredBytesWithOutputBuffer} bytes (limit {MaxDecodedAnimationBytes})";
            return false;
        }

        error = null;
        return true;
    }

    private static bool CanDecodeStaticFrame(WebPAnimationInfo info)
    {
        if (info == null || info.Width <= 0 || info.Height <= 0)
            return false;

        if (info.Width > MaxCanvasDimension || info.Height > MaxCanvasDimension)
            return false;

        return (long)info.Width * info.Height * BytesPerPixel <= MaxStaticFrameBytes;
    }

    private static void DisposeAnimatedImage(AnimatedImg animatedImg)
    {
        try
        {
            animatedImg.Dispose();
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog($"Unable to release animated WebP frames: {e}");
        }
    }

    private static void DisposeFrames(IEnumerable<AnimatedImgFrame> frames)
    {
        foreach (var frame in frames)
        {
            try
            {
                frame.Dispose();
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog($"Unable to release animated WebP frame: {e}");
            }
        }
    }
}

file static class Extension
{
    public static WriteableBitmap ToWriteableBitmap(this Bitmap bitmap)
    {
        if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

        var pixelFormat = bitmap.PixelFormat;
        var width = bitmap.Width;
        var height = bitmap.Height;

        var wpfPixelFormat = pixelFormat switch
        {
            System.Drawing.Imaging.PixelFormat.Format32bppArgb => PixelFormats.Bgra32,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb => PixelFormats.Bgr24,
            _ => throw new NotSupportedException($"Unsupported PixelFormat: {pixelFormat}")
        };

        var writeableBitmap = new WriteableBitmap(width, height, 96, 96, wpfPixelFormat, null);

        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            pixelFormat);

        try
        {
            writeableBitmap.Lock();
            unsafe
            {
                Buffer.MemoryCopy(
                    source: bitmapData.Scan0.ToPointer(),
                    destination: writeableBitmap.BackBuffer.ToPointer(),
                    destinationSizeInBytes: writeableBitmap.BackBufferStride * height,
                    sourceBytesToCopy: bitmapData.Stride * height);
            }

            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
            writeableBitmap.Unlock();
        }

        return writeableBitmap;
    }

    public static void CopyToWriteableBitmap(this Bitmap bitmap, WriteableBitmap writeableBitmap)
    {
        var pixelFormat = bitmap.PixelFormat;
        var width = bitmap.Width;
        var height = bitmap.Height;

        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            pixelFormat);

        try
        {
            writeableBitmap.Lock();
            unsafe
            {
                Buffer.MemoryCopy(
                    source: bitmapData.Scan0.ToPointer(),
                    destination: writeableBitmap.BackBuffer.ToPointer(),
                    destinationSizeInBytes: writeableBitmap.BackBufferStride * height,
                    sourceBytesToCopy: bitmapData.Stride * height);
            }

            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
            writeableBitmap.Unlock();
        }
    }
}
