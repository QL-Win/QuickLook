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

using ImageMagick;
using ImageMagick.Formats;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

/// <summary>
/// Provided for `.cur` and `.ani` cursor file
/// </summary>
internal class CursorProvider : ImageMagickProvider
{
    private bool _isPlaying;

    public CursorProvider(Uri path, MetaProvider meta, ContextObject contextObject) : base(path, meta, contextObject)
    {
    }

#if false // Not supporting thumbnails would be better
    public override Task<BitmapSource> GetThumbnail(Size renderSize)
    {
        return new Task<BitmapSource>(() =>
        {
            nint hIcon = IntPtr.Zero;

            try
            {
                hIcon = NativeMethods.ExtractIcon(IntPtr.Zero, Path.LocalPath, 0);

                if (hIcon != IntPtr.Zero)
                {
                    var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                        hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions()
                    );

                    bitmapSource.Freeze();
                    return bitmapSource;
                }
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
                return null;
            }
            finally
            {
                if (hIcon != IntPtr.Zero)
                    NativeMethods.DestroyIcon(hIcon);
            }

            return null;
        });
    }
#endif

    public override Task<BitmapSource> GetRenderedFrame(int index)
    {
        return new Task<BitmapSource>(() =>
        {
            var settings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.None,
                Defines = new DngReadDefines
                {
                    OutputColor = DngOutputColor.SRGB,
                    UseCameraWhiteBalance = true,
                    DisableAutoBrightness = false
                }
            };

            try
            {
                if (Path.LocalPath.ToLower().EndsWith(".ani"))
                {
                    return AnimatedCursor(Path.LocalPath);
                }

                return base.GetRenderedFrame();
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
                return null;
            }
        });
    }

    public override void Dispose()
    {
        _isPlaying = false;
        base.Dispose();
    }

    public BitmapSource AnimatedCursor(string path)
    {
        var aniCursor = AniCursorLoader.LoadAniCursor(path);
        var frames = aniCursor.ToArray();
        var animatedImg = new AniCursor(frames, frames.Count());

        var writeableBitmap = Application.Current.Dispatcher.Invoke(() =>
        {
            var frame = animatedImg.Frames.ElementAt(0);
            var bitmap = frame.Bitmap;
            return bitmap.ToWriteableBitmap();
        });

        _isPlaying = true;
        _ = Task.Factory.StartNew(() =>
        {
            while (_isPlaying)
            {
                foreach (var frame in animatedImg.Frames)
                {
                    if (!_isPlaying) break;

                    writeableBitmap.Dispatcher.Invoke(() =>
                    {
                        var bitmap = frame.Bitmap;
                        bitmap.CopyToWriteableBitmap(writeableBitmap);
                    });

                    Thread.Sleep((int)frame.Duration);
                }
            }

            animatedImg?.Dispose();
            animatedImg = null;
        }, TaskCreationOptions.LongRunning);

        return writeableBitmap;
    }

    public static Cursor GetCursor(string path)
    {
        try
        {
            Cursor customCursor = new(path);
            return customCursor;
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
        }
        return null;
    }
}

file static class AniCursorLoader
{
    public static IEnumerable<AniCursorFrame> LoadAniCursor(string aniFilePath)
    {
        using var fs = new FileStream(aniFilePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fs);

        reader.BaseStream.Seek(16, SeekOrigin.Begin);
        int totalFrames = reader.ReadInt16();

        // TODO: Implement animated images
        _ = totalFrames;

        // TODO: Show only first frame now
        nint hIcon = IntPtr.Zero;

        try
        {
            // Get the first frame
            hIcon = NativeMethods.ExtractIcon(IntPtr.Zero, aniFilePath, 0);

            if (hIcon != IntPtr.Zero)
            {
                using Icon icon = Icon.FromHandle(hIcon);
                Bitmap bitmap = icon.ToBitmap();
                yield return new AniCursorFrame(bitmap, 0);
            }
        }
        finally
        {
            if (hIcon != IntPtr.Zero)
                NativeMethods.DestroyIcon(hIcon);
        }
    }
}

file static class NativeMethods
{
    [DllImport("shell32.dll", SetLastError = true)]
    public static extern nint ExtractIcon(nint hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(nint hIcon);
}

file sealed class AniCursor(IEnumerable<AniCursorFrame> frames, int? frameCount = null) : IDisposable
{
    public bool IsDisposed = false;

    public void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        if (disposing)
        {
            // Free any other managed objects here.
            FrameCount = 0;
            Frames = [];
        }

        // Free any unmanaged objects here.
        IsDisposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~AniCursor()
    {
        Dispose(false);
    }

    public IEnumerable<AniCursorFrame> Frames { get; private set; } = frames;

    public int FrameCount { get; private set; } = frameCount ?? frames.Count();

    public AniCursorFrame GetFrame(int frameIndex)
    {
        try
        {
            return Frames.ElementAtOrDefault(frameIndex);
        }
        catch { }

        return null;
    }
}

file sealed class AniCursorFrame(Bitmap frame, uint duration) : IDisposable
{
    public Bitmap Bitmap { get; set; } = frame;

    public uint Duration { get; set; } = duration;

    public void Dispose()
    {
        Bitmap?.Dispose();
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
            PixelFormat.Format32bppArgb => PixelFormats.Bgra32,
            PixelFormat.Format24bppRgb => PixelFormats.Bgr24,
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
