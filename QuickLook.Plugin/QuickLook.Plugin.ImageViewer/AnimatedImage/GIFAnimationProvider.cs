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
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using QuickLook.Common.ExtensionMethods;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    internal class GIFAnimationProvider : AnimationProvider
    {
        private readonly List<FrameInfo> _decodedFrames;
        private readonly int _lastRenderedFrameIndex;
        private readonly DrawingGroup renderedFrame;

        public GIFAnimationProvider(string path) : base(path)
        {
            var decoder = new GifBitmapDecoder(new Uri(path), BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            _decodedFrames = new List<FrameInfo>(decoder.Frames.Count);
            decoder.Frames.ForEach(f => _decodedFrames.Add(GetFrameInfo(f)));

            renderedFrame = new DrawingGroup();
            _lastRenderedFrameIndex = -1;

            var delay = _decodedFrames[0].Delay.TotalMilliseconds;

            Animator = new Int32Animation(0, decoder.Frames.Count - 1,
                new Duration(TimeSpan.FromMilliseconds(delay * (decoder.Frames.Count - 1))))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
        }

        public override DrawingImage GetRenderedFrame(int index)
        {
            for (var i = _lastRenderedFrameIndex + 1; i < index; i++)
                MakeFrame(renderedFrame, _decodedFrames[i], i > 0 ? _decodedFrames[i - 1] : null);

            MakeFrame(
                renderedFrame,
                _decodedFrames[index],
                index > 0 ? _decodedFrames[index - 1] : null);
            
            var di=new DrawingImage(renderedFrame);
            di.Freeze();

            return di;
        }

        #region private methods

        private static void MakeFrame(
            DrawingGroup renderedFrame,
            FrameInfo currentFrame,
            FrameInfo previousFrame)
        {
            if (previousFrame == null)
                renderedFrame.Children.Clear();
            else
                switch (previousFrame.DisposalMethod)
                {
                    case FrameDisposalMethod.Unspecified:
                    case FrameDisposalMethod.Combine:
                        break;
                    case FrameDisposalMethod.RestorePrevious:
                        renderedFrame.Children.RemoveAt(renderedFrame.Children.Count - 1);
                        break;
                    case FrameDisposalMethod.RestoreBackground:
                        var bg = renderedFrame.Children.First();
                        renderedFrame.Children.Clear();
                        renderedFrame.Children.Add(bg);
                        break;
                }

            renderedFrame.Children.Add(new ImageDrawing(currentFrame.Frame, currentFrame.Rect));
        }

        private static FrameInfo GetFrameInfo(BitmapFrame frame)
        {
            var frameInfo = new FrameInfo
            {
                Frame = frame,
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
            public BitmapSource Frame { get; set; }
            public FrameDisposalMethod DisposalMethod { get; set; }
            public TimeSpan Delay { get; set; }
            public Rect Rect => new Rect(Left, Top, Width, Height);

            public double Width { private get; set; }
            public double Height { private get; set; }
            public double Left { private get; set; }
            public double Top { private get; set; }
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