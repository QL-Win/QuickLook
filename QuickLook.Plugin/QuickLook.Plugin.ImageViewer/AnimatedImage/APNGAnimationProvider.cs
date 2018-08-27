// Copyright © 2018 Paddy Xu
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LibAPNG;
using QuickLook.Common.ExtensionMethods;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    internal class APNGAnimationProvider : AnimationProvider
    {
        private readonly List<FrameInfo> _frames;
        private readonly List<BitmapSource> _renderedFrames;
        private NETImageProvider _imageMagickProvider;
        private int _lastEffecitvePreviousPreviousFrameIndex;

        public APNGAnimationProvider(string path) : base(path)
        {
            var decoder = new APNGBitmap(path);

            if (decoder.IsSimplePNG)
            {
                _imageMagickProvider = new NETImageProvider(path);
                return;
            }

            _frames = new List<FrameInfo>(decoder.Frames.Length);
            _renderedFrames = new List<BitmapSource>(decoder.Frames.Length);
            Enumerable.Repeat(0, decoder.Frames.Length).ForEach(_ => _renderedFrames.Add(null));

            Animator = new Int32AnimationUsingKeyFrames {RepeatBehavior = RepeatBehavior.Forever};

            var wallclock = TimeSpan.Zero;

            for (var i = 0; i < decoder.Frames.Length; i++)
            {
                var frame = decoder.Frames[i];

                _frames.Add(new FrameInfo(decoder.IHDRChunk, frame));

                Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(i, KeyTime.FromTimeSpan(wallclock)));
                wallclock += _frames[i].Delay;
            }
        }

        public override Task<BitmapSource> GetThumbnail(Size size, Size fullSize)
        {
            throw new NotImplementedException();
        }

        public override Task<BitmapSource> GetRenderedFrame(int index)
        {
            if (_imageMagickProvider != null)
                return _imageMagickProvider.GetRenderedFrame(index);

            if (_renderedFrames[index] != null)
                return new Task<BitmapSource>(() => _renderedFrames[index]);

            return new Task<BitmapSource>(() =>
            {
                var rendered = Render(index);
                _renderedFrames[index] = rendered;

                return rendered;
            });
        }

        public override void Dispose()
        {
            if (_imageMagickProvider != null)
            {
                _imageMagickProvider.Dispose();
                _imageMagickProvider = null;
                return;
            }

            _frames.Clear();
            _renderedFrames.Clear();
        }

        private BitmapSource Render(int index)
        {
            var currentFrame = _frames[index];
            FrameInfo previousFrame = null;
            BitmapSource previousRendered = null;
            BitmapSource previousPreviousRendered = null;

            if (index > 0)
            {
                if (_renderedFrames[index - 1] == null)
                    _renderedFrames[index - 1] = Render(index - 1);

                previousFrame = _frames[index - 1];
                previousRendered = _renderedFrames[index - 1];
            }

            // when saying APNGDisposeOpPrevious, we need to find the last frame not having APNGDisposeOpPrevious.
            // Only [index-2] is not correct here since that frame may also have APNGDisposeOpPrevious.
            if (index > 1)
                previousPreviousRendered = _renderedFrames[_lastEffecitvePreviousPreviousFrameIndex];
            if (_frames[index].DisposeOp != DisposeOps.APNGDisposeOpPrevious)
                _lastEffecitvePreviousPreviousFrameIndex = Math.Max(_lastEffecitvePreviousPreviousFrameIndex, index);

            var visual = new DrawingVisual();

            using (var context = visual.RenderOpen())
            {
                // protect region
                if (currentFrame.BlendOp == BlendOps.APNGBlendOpSource)
                {
                    var freeRegion = new CombinedGeometry(GeometryCombineMode.Xor,
                        new RectangleGeometry(currentFrame.FrameRect),
                        new RectangleGeometry(currentFrame.FrameRect));
                    context.PushOpacityMask(
                        new DrawingBrush(new GeometryDrawing(Brushes.Transparent, null, freeRegion)));
                }

                if (previousFrame != null)
                    switch (previousFrame.DisposeOp)
                    {
                        case DisposeOps.APNGDisposeOpNone:
                            if (previousRendered != null)
                                context.DrawImage(previousRendered, currentFrame.FullRect);
                            break;
                        case DisposeOps.APNGDisposeOpPrevious:
                            if (previousPreviousRendered != null)
                                context.DrawImage(previousPreviousRendered, currentFrame.FullRect);
                            break;
                        case DisposeOps.APNGDisposeOpBackground:
                            // do nothing
                            break;
                    }

                // unprotect region and draw current frame
                if (currentFrame.BlendOp == BlendOps.APNGBlendOpSource)
                    context.Pop();
                context.DrawImage(currentFrame.Pixels, currentFrame.FrameRect);
            }

            var bitmap = new RenderTargetBitmap(
                (int) currentFrame.FullRect.Width, (int) currentFrame.FullRect.Height,
                Math.Floor(currentFrame.Pixels.DpiX), Math.Floor(currentFrame.Pixels.DpiY),
                PixelFormats.Pbgra32);
            bitmap.Render(visual);

            bitmap.Freeze();
            return bitmap;
        }

        private class FrameInfo
        {
            public readonly BlendOps BlendOp;
            public readonly TimeSpan Delay;
            public readonly DisposeOps DisposeOp;
            public readonly Rect FrameRect;
            public readonly Rect FullRect;
            public readonly BitmapSource Pixels;

            public FrameInfo(IHDRChunk header, Frame frame)
            {
                FullRect = new Rect(0, 0, header.Width, header.Height);
                FrameRect = new Rect(frame.fcTLChunk.XOffset, frame.fcTLChunk.YOffset,
                    frame.fcTLChunk.Width, frame.fcTLChunk.Height);

                BlendOp = frame.fcTLChunk.BlendOp;
                DisposeOp = frame.fcTLChunk.DisposeOp;

                Pixels = frame.GetBitmapSource();
                Pixels.Freeze();

                Delay = TimeSpan.FromSeconds((double) frame.fcTLChunk.DelayNum /
                                             (frame.fcTLChunk.DelayDen == 0
                                                 ? 100
                                                 : frame.fcTLChunk.DelayDen));
            }
        }
    }
}