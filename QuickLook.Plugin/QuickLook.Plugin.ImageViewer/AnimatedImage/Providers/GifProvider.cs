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

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Size = System.Windows.Size;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

internal class GifProvider : AnimationProvider
{
    private readonly int FRAME_DELAY_TAG = 0x5100;

    private Stream _stream;
    private Bitmap _bitmap;
    private BitmapSource _frame;
    private bool _isPlaying;
    private NativeProvider _nativeProvider;

    private int _frameCount = 0;
    private int _frameIndex = 0;

    public GifProvider(Uri path, MetaProvider meta, ContextObject contextObject) : base(path, meta, contextObject)
    {
        if (!ImageAnimator.CanAnimate(Image.FromFile(path.LocalPath)))
        {
            _nativeProvider = new NativeProvider(path, meta, contextObject);
            return;
        }

        _stream = new FileStream(path.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        _bitmap = new Bitmap(_stream);

        _bitmap.SetResolution(DisplayDeviceHelper.DefaultDpi * DisplayDeviceHelper.GetCurrentScaleFactor().Horizontal,
            DisplayDeviceHelper.DefaultDpi * DisplayDeviceHelper.GetCurrentScaleFactor().Vertical);

        Animator = new Int32AnimationUsingKeyFrames { RepeatBehavior = RepeatBehavior.Forever };

        _frameCount = _bitmap.GetFrameCount(FrameDimension.Time);
        var frameDelayData = _bitmap.GetPropertyItem(FRAME_DELAY_TAG)?.Value;

        for (int i = 0; i < _frameCount; i++)
        {
            var frameDelays = BitConverter.ToInt32(frameDelayData, i * 4) * 10; // in millisecond
            Animator.KeyFrames.Add(new LinearInt32KeyFrame(i, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(frameDelays))));
        }
    }

    public override void Dispose()
    {
        _nativeProvider?.Dispose();
        _nativeProvider = null;

        ImageAnimator.StopAnimate(_bitmap, OnFrameChanged);
        _stream?.Dispose();
        _bitmap?.Dispose();

        _bitmap = null;
        _stream = null;
        _frame = null;
    }

    public override Task<BitmapSource> GetThumbnail(Size renderSize)
    {
        if (_nativeProvider != null)
            return _nativeProvider.GetThumbnail(renderSize);

        return new Task<BitmapSource>(() =>
        {
            _frame = _bitmap.ToBitmapSource();
            return _frame;
        });
    }

    public override Task<BitmapSource> GetRenderedFrame(int index)
    {
        if (_nativeProvider != null)
            return _nativeProvider.GetRenderedFrame(index);

        return new Task<BitmapSource>(() =>
        {
            if (!_isPlaying)
            {
                _isPlaying = true;
                ImageAnimator.Animate(_bitmap, OnFrameChanged);
            }

            return _frame;
        });
    }

    private void OnFrameChanged(object sender, EventArgs e)
    {
        _frameIndex++;
        if (_frameIndex >= _frameCount) _frameIndex = 0;

        _bitmap.SetActiveTimeFrame(_frameIndex);
        _frame = _bitmap.ToBitmapSource();
    }
}

file static class GifBitmapExtension
{
    /// <summary>
    /// Sets the active frame of the bitmap using <see cref="FrameDimension.Time"/>.
    /// </summary>
    public static void SetActiveTimeFrame(this Bitmap bmp, int frameIndex)
    {
        if (bmp == null || frameIndex < 0) return;

        var frameCount = bmp.GetFrameCount(FrameDimension.Time);

        // Check if frame index is greater than upper limit
        if (frameIndex >= frameCount) return;

        // Set active frame index
        bmp.SelectActiveFrame(FrameDimension.Time, frameIndex);
    }
}
