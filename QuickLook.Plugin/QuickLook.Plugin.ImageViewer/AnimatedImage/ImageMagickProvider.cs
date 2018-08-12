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

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    internal class ImageMagickProvider : AnimationProvider
    {
        public ImageMagickProvider(string path, NConvert meta, Dispatcher uiDispatcher) : base(path, meta, uiDispatcher)
        {
            Animator = new Int32AnimationUsingKeyFrames();
            Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(0,
                KeyTime.FromTimeSpan(TimeSpan.Zero))); // thumbnail/full image
        }

        public override Task<BitmapSource> GetRenderedFrame(int index)
        {
            return new Task<BitmapSource>(() =>
            {
                using (var ms = Meta.GetPngStream())
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.StreamSource = ms;
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    img.Freeze();

                    return img;
                }
            });
        }

        public override void Dispose()
        {
        }
    }
}