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

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    internal class GIFAnimationProvider : IAnimationProvider
    {
        public void GetAnimator(ObjectAnimationUsingKeyFrames animator, string path)
        {
            var decoder =
                new GifBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

            var clock = TimeSpan.Zero;
            BitmapSource prevFrame = null;
            FrameInfo prevInfo = null;
            BitmapSource prevprevFrame = null;
            foreach (var rawFrame in decoder.Frames)
            {
                var info = GetFrameInfo(rawFrame);
                var frame = MakeFrame(decoder.Frames[0], rawFrame, info, prevFrame, prevInfo, prevprevFrame);
                prevprevFrame = prevFrame;
                prevFrame = frame;
                prevInfo = info;

                animator.KeyFrames.Add(new DiscreteObjectKeyFrame(frame, clock));
                clock += info.Delay;
            }

            animator.Duration = clock;
            animator.RepeatBehavior = RepeatBehavior.Forever;
        }

        #region private methods

        private static BitmapSource MakeFrame(
            BitmapSource fullImage,
            BitmapSource rawFrame, FrameInfo frameInfo,
            BitmapSource previousFrame, FrameInfo previousFrameInfo,
            BitmapSource previouspreviousFrame)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                if (previousFrameInfo != null && previousFrame != null)
                {
                    var fullRect = new Rect(0, 0, fullImage.PixelWidth, fullImage.PixelHeight);

                    switch (previousFrameInfo.DisposalMethod)
                    {
                        case FrameDisposalMethod.Unspecified:
                        case FrameDisposalMethod.Combine:
                            context.DrawImage(previousFrame, fullRect);
                            break;
                        case FrameDisposalMethod.RestorePrevious:
                            if (previouspreviousFrame != null)
                                context.DrawImage(previouspreviousFrame, fullRect);
                            break;
                        case FrameDisposalMethod.RestoreBackground:
                            break;
                    }
                }

                context.DrawImage(rawFrame, frameInfo.Rect);
            }

            var bitmap = new RenderTargetBitmap(
                fullImage.PixelWidth, fullImage.PixelHeight,
                Math.Floor(fullImage.DpiX), Math.Floor(fullImage.DpiY),
                PixelFormats.Pbgra32);
            bitmap.Render(visual);
            return bitmap;
        }

        private static FrameInfo GetFrameInfo(BitmapFrame frame)
        {
            var frameInfo = new FrameInfo
            {
                Delay = TimeSpan.FromMilliseconds(100),
                DisposalMethod = FrameDisposalMethod.Unspecified,
                Width = frame.PixelWidth,
                Height = frame.PixelHeight,
                Left = 0,
                Top = 0
            };

            try
            {
                if (frame.Metadata is BitmapMetadata metadata)
                {
                    const string delayQuery = "/grctlext/Delay";
                    const string disposalQuery = "/grctlext/Disposal";
                    const string widthQuery = "/imgdesc/Width";
                    const string heightQuery = "/imgdesc/Height";
                    const string leftQuery = "/imgdesc/Left";
                    const string topQuery = "/imgdesc/Top";

                    var delay = metadata.GetQueryOrNull<ushort>(delayQuery);
                    if (delay.HasValue)
                        frameInfo.Delay = TimeSpan.FromMilliseconds(10 * delay.Value);

                    var disposal = metadata.GetQueryOrNull<byte>(disposalQuery);
                    if (disposal.HasValue)
                        frameInfo.DisposalMethod = (FrameDisposalMethod) disposal.Value;

                    var width = metadata.GetQueryOrNull<ushort>(widthQuery);
                    if (width.HasValue)
                        frameInfo.Width = width.Value;

                    var height = metadata.GetQueryOrNull<ushort>(heightQuery);
                    if (height.HasValue)
                        frameInfo.Height = height.Value;

                    var left = metadata.GetQueryOrNull<ushort>(leftQuery);
                    if (left.HasValue)
                        frameInfo.Left = left.Value;

                    var top = metadata.GetQueryOrNull<ushort>(topQuery);
                    if (top.HasValue)
                        frameInfo.Top = top.Value;
                }
            }
            catch (NotSupportedException)
            {
            }

            return frameInfo;
        }

        #endregion

        #region structs

        private class FrameInfo
        {
            public TimeSpan Delay { get; set; }
            public FrameDisposalMethod DisposalMethod { get; set; }
            public double Width { private get; set; }
            public double Height { private get; set; }
            public double Left { private get; set; }
            public double Top { private get; set; }

            public Rect Rect => new Rect(Left, Top, Width, Height);
        }

        private enum FrameDisposalMethod
        {
            Unspecified = 0,
            Combine = 1,
            RestoreBackground = 2,
            RestorePrevious = 3
        }

        #endregion
    }

    #region extensions

    public static class Extensions
    {
        public static T? GetQueryOrNull<T>(this BitmapMetadata metadata, string query)
            where T : struct
        {
            if (metadata.ContainsQuery(query))
            {
                var value = metadata.GetQuery(query);
                if (value != null)
                    return (T) value;
            }

            return null;
        }
    }

    #endregion
}