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
using System.Windows.Threading;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    internal class NETImageProvider : AnimationProvider
    {
        public NETImageProvider(string path) : base(path)
        {
            Animator = new Int32AnimationUsingKeyFrames();
            Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(0,
                KeyTime.FromTimeSpan(TimeSpan.Zero)));
        }

        public override Task<BitmapSource> GetThumbnail(Size size, Size fullSize)
        {
            var factor = Math.Min(size.Width / 2 / fullSize.Width, size.Height / 2 / fullSize.Height);
            var decode_width = fullSize.Width * factor;

            return new Task<BitmapSource>(() =>
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(Path);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.DecodePixelWidth = (int)Math.Floor(decode_width);
                img.EndInit();

                var scaled = new TransformedBitmap(img, new ScaleTransform(1d / factor, 1d / factor));
                scaled.Freeze();
                return scaled;
            });
        }

        public override Task<BitmapSource> GetRenderedFrame(int index)
        {
            return new Task<BitmapSource>(() =>
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(Path);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();

                img.Freeze();
                return img;
            });
        }

        public override void Dispose()
        {
        }
    }
}