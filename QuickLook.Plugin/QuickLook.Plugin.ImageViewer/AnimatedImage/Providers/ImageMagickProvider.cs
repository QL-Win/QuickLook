// Copyright © 2020 Paddy Xu
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
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ImageMagick;
using ImageMagick.Formats.Dng;
using QuickLook.Common.Helpers;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers
{
    internal class ImageMagickProvider : AnimationProvider
    {
        public ImageMagickProvider(string path, MetaProvider meta) : base(path, meta)
        {
            Animator = new Int32AnimationUsingKeyFrames();
            Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(0,
                KeyTime.FromTimeSpan(TimeSpan.Zero)));
        }

        public override Task<BitmapSource> GetThumbnail(Size renderSize)
        {
            var fullSize = Meta.GetSize();
            var orientation = Meta.GetOrientation();

            return new Task<BitmapSource>(() =>
            {
                try
                {
                    using (var buffer = new MemoryStream(Meta.GetThumbnail()))
                    {
                        if (buffer.Length == 0)
                            return null;

                        var img = new BitmapImage();
                        img.BeginInit();
                        img.StreamSource = buffer;
                        img.CacheOption = BitmapCacheOption.OnLoad;
                        img.EndInit();

                        var transformed = RotateAndScaleThumbnail(img, orientation, fullSize);

                        Helper.DpiHack(transformed);
                        transformed.Freeze();
                        return transformed;
                    }
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

            return new Task<BitmapSource>(() =>
            {
                var settings = new MagickReadSettings
                {
                    Defines = new DngReadDefines
                    {
                        OutputColor = DngOutputColor.SRGB,
                        UseCameraWhitebalance = true,
                        DisableAutoBrightness = false
                    }
                };

                try
                {
                    using (var mi = new MagickImage(Path, settings))
                    {
                        var profile = mi.GetColorProfile();
                        if (mi.ColorSpace == ColorSpace.RGB || mi.ColorSpace == ColorSpace.sRGB ||
                            mi.ColorSpace == ColorSpace.scRGB)
                            if (profile?.Description != null && !profile.Description.Contains("sRGB"))
                                mi.SetProfile(ColorProfile.SRGB);

                        mi.AutoOrient();

                        if (mi.Width != (int) fullSize.Width || mi.Height != (int) fullSize.Height)
                            mi.Resize((int) fullSize.Width, (int) fullSize.Height);

                        mi.Density = new Density(DpiHelper.DefaultDpi * DpiHelper.GetCurrentScaleFactor().Horizontal,
                            DpiHelper.DefaultDpi * DpiHelper.GetCurrentScaleFactor().Vertical);

                        var img = mi.ToBitmapSourceWithDensity();

                        img.Freeze();
                        return img;
                    }
                }
                catch (Exception e)
                {
                    ProcessHelper.WriteLog(e.ToString());
                    return null;
                }
            });
        }

        public override void Dispose()
        {
        }

        private static TransformedBitmap RotateAndScaleThumbnail(BitmapImage image, Orientation orientation,
            Size fullSize)
        {
            var swap = false;

            var transforms = new TransformGroup();

            // some RAWs, like from RX100, have thumbnails already rotated.
            if (fullSize.Height >= fullSize.Width && image.PixelHeight <= image.PixelWidth ||
                fullSize.Height < fullSize.Width && image.PixelHeight > image.PixelWidth)
                switch (orientation)
                {
                    case Orientation.TopRight:
                        transforms.Children.Add(new ScaleTransform(-1, 1, 0, 0));
                        break;
                    case Orientation.BottomRight:
                        transforms.Children.Add(new RotateTransform(180));
                        break;
                    case Orientation.BottomLeft:
                        transforms.Children.Add(new ScaleTransform(1, 1, 0, 0));
                        break;
                    case Orientation.LeftTop:
                        transforms.Children.Add(new RotateTransform(90));
                        transforms.Children.Add(new ScaleTransform(-1, 1, 0, 0));
                        swap = true;
                        break;
                    case Orientation.RightTop:
                        transforms.Children.Add(new RotateTransform(90));
                        swap = true;
                        break;
                    case Orientation.RightBottom:
                        transforms.Children.Add(new RotateTransform(270));
                        transforms.Children.Add(new ScaleTransform(-1, 1, 0, 0));
                        swap = true;
                        break;
                    case Orientation.LeftBottom:
                        transforms.Children.Add(new RotateTransform(270));
                        swap = true;
                        break;
                }

            transforms.Children.Add(swap
                ? new ScaleTransform(fullSize.Width / image.PixelHeight, fullSize.Height / image.PixelWidth)
                : new ScaleTransform(fullSize.Width / image.PixelWidth, fullSize.Height / image.PixelHeight));

            return new TransformedBitmap(image, transforms);
        }
    }
}