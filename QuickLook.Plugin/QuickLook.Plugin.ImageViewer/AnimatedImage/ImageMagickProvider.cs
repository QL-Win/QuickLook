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
using System.Windows.Media.Animation;
using ImageMagick;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    internal class ImageMagickProvider : IAnimationProvider
    {
        public void GetAnimator(ObjectAnimationUsingKeyFrames animator, string path)
        {
            using (var image = new MagickImage(path))
            {
                image.AddProfile(ColorProfile.SRGB);
                image.Density = new Density(Math.Floor(image.Density.X), Math.Floor(image.Density.Y));
                image.AutoOrient();

                animator.KeyFrames.Add(new DiscreteObjectKeyFrame(image.ToBitmapSource(), TimeSpan.Zero));
                animator.Duration = Duration.Forever;
            }
        }
    }
}