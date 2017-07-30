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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    public class AnimatedImage : Image, IDisposable
    {
        public static readonly DependencyProperty AnimationUriProperty =
            DependencyProperty.Register("AnimationUri", typeof(Uri), typeof(AnimatedImage),
                new UIPropertyMetadata(null, LoadImage));

        private readonly ObjectAnimationUsingKeyFrames _animator = new ObjectAnimationUsingKeyFrames();

        public Uri AnimationUri
        {
            get => (Uri) GetValue(AnimationUriProperty);
            set => SetValue(AnimationUriProperty, value);
        }

        public void Dispose()
        {
            BeginAnimation(SourceProperty, null);
            Source = null;
            _animator.KeyFrames.Clear();
        }

        private static void LoadImage(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
        {
            var instance = obj as AnimatedImage;
            if (instance == null)
                return;

            var path = ((Uri) ev.NewValue).LocalPath;
            var ext = Path.GetExtension(path).ToLower();

            IAnimationProvider provider;

            switch (ext)
            {
                case ".gif":
                    provider = new GIFAnimationProvider();
                    break;
                case ".png":
                    provider = new APNGAnimationProvider();
                    break;
                default:
                    provider = new ImageMagickProvider();
                    break;
            }

            provider.GetAnimator(instance._animator, path);

            instance.BeginAnimation(SourceProperty, instance._animator);
        }
    }
}