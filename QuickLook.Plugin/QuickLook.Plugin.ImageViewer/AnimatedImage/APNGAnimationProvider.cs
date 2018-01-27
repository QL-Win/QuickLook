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
                new ImageMagickProvider().GetAnimator(animator, path);
                return;
            }

            var clock = TimeSpan.Zero;
            var header = decoder.IHDRChunk;
            Frame currentFrame = null;
            BitmapSource currentRenderedFrame = null;
            BitmapSource previousStateRenderedFrame = null;
            foreach (var nextFrame in decoder.Frames)
            {
                var nextRenderedFrame = MakeNextFrame(header, nextFrame, currentFrame, currentRenderedFrame,
                    previousStateRenderedFrame);

                var delay = TimeSpan.FromSeconds(
                    (double) nextFrame.fcTLChunk.DelayNum /
                    (nextFrame.fcTLChunk.DelayDen == 0 ? 100 : nextFrame.fcTLChunk.DelayDen));

                animator.KeyFrames.Add(new DiscreteObjectKeyFrame(nextRenderedFrame, clock));
                clock += delay;

                // the "previous state" of a "DisposeOpPrevious" frame is its previous frame, so we do not record it
                if (currentFrame != null && currentFrame.fcTLChunk.DisposeOp != DisposeOps.APNGDisposeOpPrevious)
                    previousStateRenderedFrame = currentRenderedFrame;
                currentRenderedFrame = nextRenderedFrame;
                currentFrame = nextFrame;
            }

            animator.Duration = clock;
            animator.RepeatBehavior = RepeatBehavior.Forever;
        }

        private static BitmapSource MakeNextFrame(IHDRChunk header, Frame nextFrame, Frame currentFrame,
            BitmapSource currentRenderedFrame, BitmapSource previousStateRenderedFrame)
        {
            var fullRect = new Rect(0, 0, header.Width, header.Height);
            var frameRect = new Rect(nextFrame.fcTLChunk.XOffset, nextFrame.fcTLChunk.YOffset,
                nextFrame.fcTLChunk.Width, nextFrame.fcTLChunk.Height);

            var fs = nextFrame.GetBitmapSource();
            var visual = new DrawingVisual();

            using (var context = visual.RenderOpen())
            {
                // protect region
                if (nextFrame.fcTLChunk.BlendOp == BlendOps.APNGBlendOpSource)
                {
                    var freeRegion = new CombinedGeometry(GeometryCombineMode.Xor,
                        new RectangleGeometry(fullRect),
                        new RectangleGeometry(frameRect));
                    context.PushOpacityMask(
                        new DrawingBrush(new GeometryDrawing(Brushes.Transparent, null, freeRegion)));
                }

                if (currentFrame != null && currentRenderedFrame != null)
                    switch (currentFrame.fcTLChunk.DisposeOp)
                    {
                        case DisposeOps.APNGDisposeOpNone:
                            // restore currentRenderedFrame
                            if (currentRenderedFrame != null) context.DrawImage(currentRenderedFrame, fullRect);
                            break;
                        case DisposeOps.APNGDisposeOpPrevious:
                            // restore previousStateRenderedFrame
                            if (previousStateRenderedFrame != null)
                                context.DrawImage(previousStateRenderedFrame, fullRect);
                            break;
                        case DisposeOps.APNGDisposeOpBackground:
                            // do nothing
                            break;
                    }

                // unprotect region and draw the next frame
                if (nextFrame.fcTLChunk.BlendOp == BlendOps.APNGBlendOpSource)
                    context.Pop();
                context.DrawImage(fs, frameRect);
            }

            var bitmap = new RenderTargetBitmap(
                header.Width, header.Height,
                Math.Floor(fs.DpiX), Math.Floor(fs.DpiY),
                PixelFormats.Pbgra32);
            bitmap.Render(visual);
            return bitmap;
        }
    }
}