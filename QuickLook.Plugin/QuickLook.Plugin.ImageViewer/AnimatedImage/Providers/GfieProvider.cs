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

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.ImageViewer.Helpers;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Size = System.Windows.Size;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

/// <summary>
/// Provider for Greenfish Icon Editor Pro native documents (.gfie / .gfi).
/// Fully renders pages by compositing layers (not thumbnail-only).
/// </summary>
internal class GfieProvider : AnimationProvider
{
    private GfieDocument _doc;
    private BitmapSource _staticFrame;
    private bool _isPlaying;
    private WriteableBitmap _animatedBitmap;

    public GfieProvider(Uri path, MetaProvider meta, ContextObject contextObject) : base(path, meta, contextObject)
    {
        Animator = new Int32AnimationUsingKeyFrames();
        Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));

        try
        {
            _doc = new GfieDocument();
            if (!_doc.Load(path.LocalPath) || _doc.Pages.Count == 0)
            {
                _doc.Clear();
                _doc = null;
                return;
            }

            if (_doc.IsAnimated())
            {
                Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(10))));
                Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(20))));
                Animator.RepeatBehavior = RepeatBehavior.Forever;
            }
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
            _doc?.Clear();
            _doc = null;
        }
    }

    public override void Dispose()
    {
        _isPlaying = false;

        _doc?.Clear();
        _doc = null;
        _staticFrame = null;
        _animatedBitmap = null;
    }

    public override Task<BitmapSource> GetThumbnail(Size renderSize)
    {
        return GetRenderedFrame(0);
    }

    public override Task<BitmapSource> GetRenderedFrame(int index)
    {
        return new Task<BitmapSource>(() =>
        {
            if (_doc == null || _doc.Pages.Count == 0)
                return null;

            try
            {
                if (_doc.IsAnimated())
                    return RenderAnimated();

                if (_staticFrame != null)
                    return _staticFrame;

                var page = _doc.GetBestPage();
                using var bmp = GfieDocument.Flatten(page);
                if (bmp == null)
                    return null;

                var frame = bmp.ToBitmapSource();
                ImageHelper.DpiHack(frame);
                frame.Freeze();
                _staticFrame = frame;
                return frame;
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
                return null;
            }
        });
    }

    private BitmapSource RenderAnimated()
    {
        if (_animatedBitmap != null)
            return _animatedBitmap;

        var pages = _doc.Pages;
        Bitmap firstBmp = null;

        try
        {
            firstBmp = GfieDocument.Flatten(pages[0]);
            if (firstBmp == null)
                return null;

            _animatedBitmap = Application.Current.Dispatcher.Invoke(() => BitmapToWriteableBitmap(firstBmp));
        }
        finally
        {
            firstBmp?.Dispose();
        }

        _isPlaying = true;
        var loopCount = _doc.Data.LoopCount; // 0 = infinite

        _ = Task.Factory.StartNew(() =>
        {
            var loops = 0;
            while (_isPlaying)
            {
                for (var i = 0; i < pages.Count; i++)
                {
                    if (!_isPlaying)
                        break;

                    using var frameBmp = GfieDocument.Flatten(pages[i]);
                    if (frameBmp == null)
                        continue;

                    try
                    {
                        _animatedBitmap?.Dispatcher.Invoke(() =>
                        {
                            if (_animatedBitmap != null && _isPlaying)
                                CopyBitmapToWriteableBitmap(frameBmp, _animatedBitmap);
                        });
                    }
                    catch
                    {
                        return;
                    }

                    var delay = pages[i].FrameRate;
                    if (delay <= 0)
                        delay = 100;
                    Thread.Sleep(delay);
                }

                loops++;
                if (loopCount > 0 && loops >= loopCount)
                    break;
            }
        }, TaskCreationOptions.LongRunning);

        return _animatedBitmap;
    }

    private static WriteableBitmap BitmapToWriteableBitmap(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        CopyBitmapToWriteableBitmap(bitmap, writeableBitmap);
        ImageHelper.DpiHack(writeableBitmap);
        return writeableBitmap;
    }

    private static void CopyBitmapToWriteableBitmap(Bitmap bitmap, WriteableBitmap writeableBitmap)
    {
        // Normalize to 32bpp ARGB for stride-safe copy
        Bitmap source = bitmap;
        Bitmap converted = null;

        try
        {
            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                converted = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(converted))
                    g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                source = converted;
            }

            var width = Math.Min(source.Width, writeableBitmap.PixelWidth);
            var height = Math.Min(source.Height, writeableBitmap.PixelHeight);

            var bitmapData = source.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                writeableBitmap.Lock();
                unsafe
                {
                    var src = (byte*)bitmapData.Scan0;
                    var dst = (byte*)writeableBitmap.BackBuffer;
                    var srcStride = bitmapData.Stride;
                    var dstStride = writeableBitmap.BackBufferStride;
                    var rowBytes = width * 4;

                    for (var y = 0; y < height; y++)
                    {
                        Buffer.MemoryCopy(src + y * srcStride, dst + y * dstStride, dstStride, rowBytes);
                    }
                }

                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                source.UnlockBits(bitmapData);
                writeableBitmap.Unlock();
            }
        }
        finally
        {
            converted?.Dispose();
        }
    }
}
