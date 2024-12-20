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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Size = System.Windows.Size;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

internal class GifProvider : AnimationProvider
{
    private readonly int FRAME_DELAY_TAG = 0x5100;
    private readonly int LOOP_COUNT_TAG = 20737;

    private Stream _stream;
    private Bitmap _bitmap;
    private BitmapSource _frame;
    private bool _isPlaying;
    private NativeProvider _nativeProvider;

    private int[] _frameDelays; // in millisecond
    private readonly int _frameCount = 0;
    private int _frameIndex = 0;
    private readonly int _maxLoopCount = 0; // 0 - infinite loop
    private int _loopIndex = 0;

    private TimeSpan _minTickTimeInMillisecond = TimeSpan.FromMilliseconds(20);

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
        _maxLoopCount = BitConverter.ToInt16(_bitmap.GetPropertyItem(LOOP_COUNT_TAG).Value, 0);

        var frameDelayData = _bitmap.GetPropertyItem(FRAME_DELAY_TAG)?.Value;
        _frameDelays = new int[_frameCount];

        for (int i = 0; i < _frameCount; i++)
        {
            _frameDelays[i] = BitConverter.ToInt32(frameDelayData, i * 4) * 10;

            // The current architecture only requires 3 frames,
            // and subsequent frames are triggered by a background thread
            // Animator.KeyFrames.Add(new LinearInt32KeyFrame(i, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(_frameDelays[i]))));
        }

        Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
        Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(10))));
        Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(20))));
    }

    public override void Dispose()
    {
        _isPlaying = false;

        _nativeProvider?.Dispose();
        _nativeProvider = null;

        try
        {
            lock (_bitmap ?? new object()) // Lock to prevent null reference exception
            {
                _bitmap?.Dispose();
                _bitmap = null;

                _stream?.Dispose();
                _stream = null;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        _frame = null;
        _frameDelays = null;
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

                BeginAnimateBackground();
            }

            return _frame;
        });
    }

    private void BeginAnimateBackground()
    {
        var _thHeartBeat = new Thread(HandleThreadHeartBeatTicked)
        {
            IsBackground = true,
            Name = "heartbeat - ImageAnimator"
        };
        _thHeartBeat.Start();
    }

    /// <summary>
    /// Given a delay amount, return either the minimum tick or delay, whichever is greater.
    /// </summary>
    /// <returns> the time to sleep during a tick in milliseconds </returns>
    private TimeSpan GetSleepAmountInMilliseconds(TimeSpan delay)
    {
        if (delay > _minTickTimeInMillisecond)
        {
            return delay;
        }

        return _minTickTimeInMillisecond;
    }

    private TimeSpan GetFrameDelay(int frameIndex)
    {
        return TimeSpan.FromMilliseconds(_frameDelays[frameIndex]);
    }

    /// <summary>
    /// Process image frame tick.
    /// </summary>
    private void HandleThreadHeartBeatTicked()
    {
        var initSleepTime = GetSleepAmountInMilliseconds(GetFrameDelay(_frameIndex));
        Thread.Sleep(initSleepTime);

        while (_isPlaying)
        {
            try
            {
                UpdateFrame(_frameIndex);

                var sleepTime = GetSleepAmountInMilliseconds(GetFrameDelay(_frameIndex));
                Thread.Sleep(sleepTime);
            }
            catch (ArgumentException)
            {
                // ignore errors that occur due to the image being disposed
            }
            catch (OutOfMemoryException)
            {
                // also ignore errors that occur due to running out of memory
            }
            catch (ExternalException)
            {
                // ignore
            }
            catch (InvalidOperationException)
            {
                // ignore
            }

            _frameIndex++;
            if (_frameIndex >= _frameCount)
            {
                _frameIndex = 0;
                _loopIndex++;

                if (_maxLoopCount > 0 && _loopIndex >= _maxLoopCount)
                {
                    _isPlaying = false;
                    return;
                }
            }
        }
    }

    private void UpdateFrame(int frameIndex)
    {
        lock (_bitmap)
        {
            _bitmap.SetActiveTimeFrame(frameIndex);
            _frame = _bitmap.ToBitmapSource();
        }
    }
}

file static class BitmapExtensions
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
