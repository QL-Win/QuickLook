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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using QuickLook.Common.Helpers;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers
{
    internal class NativeProvider : AnimationProvider
    {
        public NativeProvider(string path, MetaProvider meta) : base(path, meta)
        {
            Animator = new Int32AnimationUsingKeyFrames();
            Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(0,
                KeyTime.FromTimeSpan(TimeSpan.Zero)));
        }

        public override Task<BitmapSource> GetThumbnail(Size renderSize)
        {
            var fullSize = Meta.GetSize();

            //var decodeWidth = (int) Math.Round(fullSize.Width *
            //                                   Math.Min(renderSize.Width / 2 / fullSize.Width,
            //                                       renderSize.Height / 2 / fullSize.Height));
            //var decodeHeight = (int) Math.Round(fullSize.Height / fullSize.Width * decodeWidth);
            var decodeWidth =
                (int) Math.Round(Math.Min(Meta.GetSize().Width, Math.Max(1d, Math.Floor(renderSize.Width))));
            var decodeHeight =
                (int) Math.Round(Math.Min(Meta.GetSize().Height, Math.Max(1d, Math.Floor(renderSize.Height))));
            var orientation = Meta.GetOrientation();
            var rotate = ShouldRotate(orientation);

            return new Task<BitmapSource>(() =>
            {
                try
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(Path);
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    // specific renderSize to avoid .net's double to int conversion
                    img.DecodePixelWidth = rotate ? decodeHeight : decodeWidth;
                    img.DecodePixelHeight = rotate ? decodeWidth : decodeHeight;
                    img.EndInit();

                    var scaled = rotate
                        ? new TransformedBitmap(img,
                            new ScaleTransform(fullSize.Height / img.PixelWidth, fullSize.Width / img.PixelHeight))
                        : new TransformedBitmap(img,
                            new ScaleTransform(fullSize.Width / img.PixelWidth, fullSize.Height / img.PixelHeight));

                    var rotated = ApplyTransformFromExif(scaled, orientation);

                    Helper.DpiHack(rotated);
                    rotated.Freeze();

                    return rotated;
                }
                catch (Exception e)
                {
                    ProcessHelper.WriteLog(e.ToString());
                    return null;
                }
            });
        }

        public override Task<BitmapSource> GetRenderedFrame(int index)
        {
            var fullSize = Meta.GetSize();
            var rotate = ShouldRotate(Meta.GetOrientation());

            return new Task<BitmapSource>(() =>
            {
                try
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(Path);
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.DecodePixelWidth = (int) (rotate ? fullSize.Height : fullSize.Width);
                    img.DecodePixelHeight = (int) (rotate ? fullSize.Width : fullSize.Height);
                    img.EndInit();

                    var img2 = ApplyTransformFromExif(img, Meta.GetOrientation());

                    Helper.DpiHack(img2);
                    img2.Freeze();

                    return img2;
                }
                catch (Exception e)
                {
                    ProcessHelper.WriteLog(e.ToString());
                    return null;
                }
            });
        }

        private static bool ShouldRotate(Orientation orientation)
        {
            var rotate = false;
            switch (orientation)
            {
                case Orientation.LeftTop:
                case Orientation.RightTop:
                case Orientation.RightBottom:
                case Orientation.LeftBottom:
                    rotate = true;
                    break;
            }

            return rotate;
        }

        private static BitmapSource ApplyTransformFromExif(BitmapSource image, Orientation orientation)
        {
            switch (orientation)
            {
                case Orientation.Undefined:
                case Orientation.TopLeft:
                    return image;
                case Orientation.TopRight:
                    return new TransformedBitmap(image, new ScaleTransform(-1, 1, 0, 0));
                case Orientation.BottomRight:
                    return new TransformedBitmap(image, new RotateTransform(180));
                case Orientation.BottomLeft:
                    return new TransformedBitmap(image, new ScaleTransform(1, 1, 0, 0));
                case Orientation.LeftTop:
                    return new TransformedBitmap(
                        new TransformedBitmap(image, new RotateTransform(90)),
                        new ScaleTransform(-1, 1, 0, 0));
                case Orientation.RightTop:
                    return new TransformedBitmap(image, new RotateTransform(90));
                case Orientation.RightBottom:
                    return new TransformedBitmap(
                        new TransformedBitmap(image, new RotateTransform(270)),
                        new ScaleTransform(-1, 1, 0, 0));
                case Orientation.LeftBottom:
                    return new TransformedBitmap(image, new RotateTransform(270));
            }

            return image;
        }

        public override void Dispose()
        {
        }
    }
}