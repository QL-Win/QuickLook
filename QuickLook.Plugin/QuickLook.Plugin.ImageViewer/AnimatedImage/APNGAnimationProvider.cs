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

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using LibAPNG;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    internal class APNGAnimationProvider : IAnimationProvider
    {
        public void GetAnimator(ObjectAnimationUsingKeyFrames animator, string path)
        {
            var decoder = new APNGBitmap(path);

            if (decoder.IsSimplePNG)
            {
                animator.KeyFrames.Add(
                    new DiscreteObjectKeyFrame(decoder.DefaultImage.GetBitmapSource(), TimeSpan.Zero));
                animator.Duration = Duration.Forever;
                return;
            }

            var clock = TimeSpan.Zero;
            var header = decoder.IHDRChunk;
            Frame prevFrame = null;
            BitmapSource prevRenderedFrame = null;
            foreach (var rawFrame in decoder.Frames)
            {
                var frame = MakeFrame(header, rawFrame, prevFrame, prevRenderedFrame);
                prevFrame = rawFrame;
                prevRenderedFrame = frame;

                var delay = TimeSpan.FromSeconds(
                    (double) rawFrame.fcTLChunk.DelayNum /
                    (rawFrame.fcTLChunk.DelayDen == 0 ? 100 : rawFrame.fcTLChunk.DelayDen));

                animator.KeyFrames.Add(new DiscreteObjectKeyFrame(frame, clock));
                clock += delay;
            }

            animator.Duration = clock;
            animator.RepeatBehavior = RepeatBehavior.Forever;
        }

        private static BitmapSource MakeFrame(IHDRChunk header, Frame rawFrame, Frame previousFrame,
            BitmapSource previousRenderedFrame)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                switch (rawFrame.fcTLChunk.DisposeOp)
                {
                    case DisposeOps.APNGDisposeOpNone:
                        // restore previousRenderedFrame
                        //if (previousRenderedFrame != null)
                        //{
                        //    var fullRect = new Rect(0, 0, header.Width, header.Height);
                        //    context.DrawImage(previousRenderedFrame, fullRect);
                        //}
                        break;
                    case DisposeOps.APNGDisposeOpPrevious:
                        // restore previousFrame
                        if (previousFrame != null)
                        {
                            var pFrameRect = new Rect(previousFrame.fcTLChunk.XOffset,
                                previousFrame.fcTLChunk.YOffset,
                                previousFrame.fcTLChunk.Width, previousFrame.fcTLChunk.Height);
                            context.DrawImage(previousFrame.GetBitmapSource(), pFrameRect);
                        }
                        break;
                    case DisposeOps.APNGDisposeOpBackground:
                        // do nothing
                        break;
                }

                // draw current frame
                var frameRect = new Rect(rawFrame.fcTLChunk.XOffset, rawFrame.fcTLChunk.YOffset,
                    rawFrame.fcTLChunk.Width, rawFrame.fcTLChunk.Height);
                context.DrawImage(rawFrame.GetBitmapSource(), frameRect);
            }

            var bitmap = new RenderTargetBitmap(
                header.Width, header.Height,
                96, 96,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);
            return bitmap;
        }
    }
}