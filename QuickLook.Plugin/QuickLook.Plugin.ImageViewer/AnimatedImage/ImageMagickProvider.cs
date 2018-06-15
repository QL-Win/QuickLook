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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ImageMagick;
using QuickLook.Plugin.ImageViewer.Exiv2;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    internal class ImageMagickProvider : AnimationProvider
    {
        private readonly string _path;
        private readonly BitmapSource _thumbnail;

        public ImageMagickProvider(string path, Dispatcher uiDispatcher) : base(path, uiDispatcher)
        {
            _path = path;
            _thumbnail = new Meta(path).GetThumbnail(true);

            Animator = new Int32AnimationUsingKeyFrames();
            Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(0,
                KeyTime.FromTimeSpan(TimeSpan.Zero))); // thumbnail/full image

            if (_thumbnail != null)
                Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(1,
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.20)))); // full image
        }

        public override Task<BitmapSource> GetRenderedFrame(int index)
        {
            // the first image is always returns synchronously.
            if (index == 0 && _thumbnail != null) return new Task<BitmapSource>(() => _thumbnail);

            return new Task<BitmapSource>(() =>
            {
                using (var image = new MagickImage(_path))
                {
                    image.AddProfile(ColorProfile.SRGB);
                    image.Density = new Density(Math.Floor(image.Density.X), Math.Floor(image.Density.Y));
                    image.AutoOrient();

                    var bs = image.ToBitmapSource();
                    bs.Freeze();
                    return bs;
                }
            });
        }

        public override void Dispose()
        {
        }
    }
}