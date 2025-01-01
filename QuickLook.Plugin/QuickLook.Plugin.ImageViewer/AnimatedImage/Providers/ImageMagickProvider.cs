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

using ImageMagick;
using ImageMagick.Formats;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using MediaPixelFormats = System.Windows.Media.PixelFormats;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

internal class ImageMagickProvider : AnimationProvider
{
    public ImageMagickProvider(Uri path, MetaProvider meta, ContextObject contextObject) : base(path, meta, contextObject)
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
        return new Task<BitmapSource>(GetRenderedFrame);
    }

    protected virtual BitmapSource GetRenderedFrame()
    {
        var fullSize = Meta.GetSize();
        var settings = new MagickReadSettings
        {
            BackgroundColor = MagickColors.None,
            Defines = new DngReadDefines
            {
                OutputColor = DngOutputColor.SRGB,
                UseCameraWhiteBalance = true,
                DisableAutoBrightness = false
            }
        };

        try
        {
            using (MagickImageCollection layers = new MagickImageCollection(Path.LocalPath, settings))
            {
                IMagickImage<byte> mi;
                // Only flatten multi-layer gimp xcf files.
                if (Path.LocalPath.ToLower().EndsWith(".xcf") && layers.Count > 1)
                {
                    // Flatten crops layers to canvas
                    mi = layers.Flatten(MagickColor.FromRgba(0, 0, 0, 0));
                }
                else
                {
                    mi = layers[0];
                }
                if (SettingHelper.Get("UseColorProfile", false, "QuickLook.Plugin.ImageViewer"))
                {
                    if (mi.ColorSpace == ColorSpace.RGB || mi.ColorSpace == ColorSpace.sRGB || mi.ColorSpace == ColorSpace.scRGB)
                    {
                        mi.SetProfile(ColorProfile.SRGB);
                        if (ContextObject.ColorProfileName != null)
                            mi.SetProfile(new ColorProfile(ContextObject.ColorProfileName)); // map to monitor color
                    }
                }

                mi.AutoOrient();

                if (mi.Width != (int)fullSize.Width || mi.Height != (int)fullSize.Height)
                    mi.Resize((uint)fullSize.Width, (uint)fullSize.Height);

                mi.Density = new Density(DisplayDeviceHelper.DefaultDpi * DisplayDeviceHelper.GetCurrentScaleFactor().Horizontal,
                    DisplayDeviceHelper.DefaultDpi * DisplayDeviceHelper.GetCurrentScaleFactor().Vertical);

                var img = mi.ToBitmapSourceWithDensity();

                img.Freeze();
                return img;
            }
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
            return null!;
        }
    }

    public override void Dispose()
    {
    }

    protected static TransformedBitmap RotateAndScaleThumbnail(BitmapImage image, Orientation orientation,
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

    protected bool IsImageMagickSupported(string path)
    {
        try
        {
            return new MagickImageInfo(path).Format != MagickFormat.Unknown;
        }
        catch
        {
            return false;
        }
    }
}

file static class Extension
{
    /// <summary>
    /// https://github.com/dlemstra/Magick.NET/blob/main/src/Magick.NET.SystemWindowsMedia/IMagickImageExtentions.cs
    /// </summary>
    public static BitmapSource ToBitmapSourceWithDensity(this IMagickImage<byte> self, bool useDensity = true)
    {
        var image = self;

        var mapping = "RGB";
        var format = MediaPixelFormats.Rgb24;

        try
        {
            if (self.ColorSpace == ColorSpace.CMYK && !image.HasAlpha)
            {
                mapping = "CMYK";
                format = MediaPixelFormats.Cmyk32;
            }
            else
            {
                if (image.ColorSpace != ColorSpace.sRGB)
                {
                    image = self.Clone();
                    image.ColorSpace = ColorSpace.sRGB;
                }

                if (image.HasAlpha)
                {
                    mapping = "BGRA";
                    format = MediaPixelFormats.Bgra32;
                }
            }

            var step = format.BitsPerPixel / 8;
            var stride = (int)image.Width * step;

            using var pixels = image.GetPixelsUnsafe();
            var bytes = pixels.ToByteArray(mapping);
            var dpi = GetDefaultDensity(image, useDensity ? DensityUnit.PixelsPerInch : DensityUnit.Undefined);
            return BitmapSource.Create((int)image.Width, (int)image.Height, dpi.X, dpi.Y, format, null, bytes, stride);
        }
        finally
        {
            if (!ReferenceEquals(self, image))
                image.Dispose();
        }
    }

    private static Density GetDefaultDensity(IMagickImage image, DensityUnit units)
    {
        if (units == DensityUnit.Undefined || (image.Density.X <= 0 || image.Density.Y <= 0))
            return new Density(96);

        return image.Density.ChangeUnits(units);
    }
}
