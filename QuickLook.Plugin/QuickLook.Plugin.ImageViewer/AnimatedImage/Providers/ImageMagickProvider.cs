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
                        //// specific renderSize to avoid .net's double to int conversion
                        //img.DecodePixelWidth = Math.Max(1, (int) Math.Floor(renderSize.Width));
                        //img.DecodePixelHeight = Math.Max(1, (int) Math.Floor(renderSize.Height));
                        img.EndInit();

                        var scaled = new TransformedBitmap(img,
                            new ScaleTransform(fullSize.Width / img.PixelWidth, fullSize.Height / img.PixelHeight));

                        Helper.DpiHack(scaled);
                        scaled.Freeze();
                        return scaled;
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
                        if (mi.ColorSpace == ColorSpace.RGB || mi.ColorSpace == ColorSpace.sRGB || mi.ColorSpace == ColorSpace.scRGB)
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
    }
}