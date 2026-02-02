// Copyright © 2017-2026 QL-Win Contributors
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

using FellowOakDicom;
using FellowOakDicom.Imaging;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

internal sealed class DicomProvider : AnimationProvider
{
    private static readonly HashSet<DicomTransferSyntax> SupportedTransferSyntaxes =
    [
        DicomTransferSyntax.ImplicitVRLittleEndian,
        DicomTransferSyntax.ExplicitVRLittleEndian,
        DicomTransferSyntax.ExplicitVRBigEndian,
    ];

    private static readonly object ImageManagerLock = new();
    private static bool _imageManagerInitialized;

    private readonly DicomDataset _dataset;
    private readonly DicomImage _dicomImage;
    private readonly object _renderLock = new();

    public DicomProvider(Uri path, MetaProvider meta, ContextObject contextObject) : base(path, meta, contextObject)
    {
        EnsureWpfImageManager();

        _dataset = LoadDicomDataset(path.LocalPath);
        if (_dataset == null)
            return;

        try
        {
            _dicomImage = new DicomImage(_dataset);
            Animator = new Int32AnimationUsingKeyFrames();

            var duration = GetFrameDuration(_dataset);
            var accumulator = TimeSpan.Zero;

            for (var i = 0; i < _dicomImage.NumberOfFrames; i++)
            {
                Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(i, KeyTime.FromTimeSpan(accumulator)));
                accumulator += duration;
            }

            if (accumulator == TimeSpan.Zero && _dicomImage.NumberOfFrames > 0)
                accumulator = TimeSpan.FromMilliseconds(100);

            Animator.Duration = new Duration(accumulator);
            Animator.RepeatBehavior = RepeatBehavior.Forever;
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
            _dicomImage = null;
        }
    }

    public override Task<BitmapSource> GetThumbnail(Size renderSize)
    {
        return new Task<BitmapSource>(() =>
        {
            if (_dicomImage == null)
                return null;

            lock (_renderLock)
            {
                try
                {
                    using var rendered = _dicomImage.RenderImage(0);
                    var image = TryGetBitmapSource(rendered);
                    if (image == null)
                        return null;

                    var scaled = ScaleToFit(image, renderSize);
                    Helper.DpiHack(scaled);
                    scaled.Freeze();

                    return scaled;
                }
                catch (Exception e)
                {
                    ProcessHelper.WriteLog(e.ToString());
                    return null;
                }
            }
        });
    }

    public override Task<BitmapSource> GetRenderedFrame(int index)
    {
        return new Task<BitmapSource>(() =>
        {
            if (_dicomImage == null)
                return null;

            lock (_renderLock)
            {
                try
                {
                    if (index < 0 || index >= _dicomImage.NumberOfFrames)
                        return null;

                    using var rendered = _dicomImage.RenderImage(index);
                    var image = TryGetBitmapSource(rendered);
                    if (image == null)
                        return null;

                    Helper.DpiHack(image);
                    image.Freeze();

                    return image;
                }
                catch (Exception e)
                {
                    ProcessHelper.WriteLog(e.ToString());
                    return null;
                }
            }
        });
    }

    public override void Dispose()
    {
        // DicomImage logic does not require disposal, it holds the Dataset which is in memory.
    }

    private DicomDataset LoadDicomDataset(string path)
    {
        try
        {
            var file = DicomFile.Open(path);
            var transferSyntax = file.FileMetaInfo?.TransferSyntax ?? file.Dataset.InternalTransferSyntax;

            // Strict check
            if (!SupportedTransferSyntaxes.Contains(transferSyntax))
            {
                return null;
            }

            var pixelData = DicomPixelData.Create(file.Dataset, false);
            if (!IsSupportedPixelData(pixelData))
                return null;

            return file.Dataset;
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
            return null;
        }
    }

    private static TimeSpan GetFrameDuration(DicomDataset dataset)
    {
        if (TryGetSingleValue(dataset, DicomTag.FrameTime, out double frameTime))
        {
            return TimeSpan.FromMilliseconds(frameTime);
        }

        if (TryGetSingleValue(dataset, DicomTag.RecommendedDisplayFrameRate, out double frameRate) && frameRate > 0)
        {
            // FPS to duration
            return TimeSpan.FromSeconds(1.0 / frameRate);
        }

        return TimeSpan.FromMilliseconds(100);
    }

    private static bool TryGetSingleValue<T>(DicomDataset dataset, DicomTag tag, out T value)
    {
        value = default;
        try
        {
            if (dataset.Contains(tag))
            {
                value = dataset.GetValue<T>(tag, 0); // Use GetValue with index 0 which is safer or simple GetValue<T>(tag, 0)
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static BitmapSource TryGetBitmapSource(IImage image)
    {
        try
        {
            var wpfBitmap = image.AsWriteableBitmap();
            return wpfBitmap;
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog($"Bitmap conversion failed: {e}");
            return null;
        }
    }

    private static void EnsureWpfImageManager()
    {
        if (_imageManagerInitialized)
            return;

        lock (ImageManagerLock)
        {
            if (_imageManagerInitialized)
                return;

            try
            {
                new DicomSetupBuilder()
                    .RegisterServices(s =>
                    {
                        s.AddFellowOakDicom();
                        s.AddImageManager<WPFImageManager>();
                    })
                    .Build();
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog($"DICOM Init Failed: {e}");
            }

            _imageManagerInitialized = true;
        }
    }

    private static bool IsSupportedPixelData(DicomPixelData pixelData)
    {
        var photometric = pixelData.PhotometricInterpretation;

        if (photometric == PhotometricInterpretation.YbrFull422 ||
            photometric == PhotometricInterpretation.YbrPartial422)
            return false;

        return photometric == PhotometricInterpretation.Monochrome1 ||
            photometric == PhotometricInterpretation.Monochrome2 ||
            photometric == PhotometricInterpretation.Rgb ||
            photometric == PhotometricInterpretation.YbrFull;
    }

    private static BitmapSource ScaleToFit(BitmapSource image, Size renderSize)
    {
        if (image == null)
            return null;

        if (renderSize.Width <= 0 || renderSize.Height <= 0)
            return image;

        var scale = Math.Min(renderSize.Width / image.PixelWidth, renderSize.Height / image.PixelHeight);
        if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0 || scale >= 1)
            return image;

        return new TransformedBitmap(image, new ScaleTransform(scale, scale));
    }
}
