// Copyright © 2024 QL-Win Contributors
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
using ImageMagick;
using ImageMagick.Formats;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

internal class WebPProvider : ImageMagickProvider
{
    private bool _isPlaying;

    public WebPProvider(Uri path, MetaProvider meta, ContextObject contextObject) : base(path, meta, contextObject)
    {
    }

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
                // Unfortunately we only support Animated WebP on x64 platforms
                if (Environment.Is64BitProcess)
                {
                    var layers = MagickImageInfo.ReadCollection(Path.LocalPath);
                    int count = layers.Count();

                    // Animated WebP image
                    if (count > 1)
                    {
                        return AnimatedWebP(Path.LocalPath);
                    }
                    else
                    {
                        return base.GetRenderedFrame();
                    }
                }

                return base.GetRenderedFrame();
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
                return null!;
            }
        });
    }

    public override void Dispose()
    {
        _isPlaying = false;
        base.Dispose();
    }

    private BitmapSource AnimatedWebP(string fileName)
    {
        using var webP = new WebPWrapper();

        var aniWebP = webP.AnimLoad(fileName);
        var frames = aniWebP.Select(frame =>
        {
            var duration = frame.Duration > 0 ? frame.Duration : 100;
            var bitmap = frame.Bitmap;

            return new AnimatedImgFrame(frame.Bitmap, (uint)duration);
        });

        var animatedImg = new AnimatedImg(frames, frames.Count());

        var writeableBitmap = Application.Current.Dispatcher.Invoke(() =>
        {
            var frame = animatedImg.Frames.ElementAt(0);
            var bitmap = (Bitmap)frame.Bitmap;
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
                        var bitmap = (Bitmap)frame.Bitmap;
                        bitmap.CopyToWriteableBitmap(writeableBitmap);
                    });

                    Thread.Sleep((int)frame.Duration.TotalMilliseconds);
                }
            }

            animatedImg?.Dispose();
            animatedImg = null;
        }, TaskCreationOptions.LongRunning);

        return writeableBitmap;
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
