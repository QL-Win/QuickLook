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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using QuickLook.Common.Helpers;
using QuickLook.Plugin.ImageViewer.Exiv2;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    public class AnimatedImage : Image, IDisposable
    {
        private AnimationProvider _animation;
        private bool _disposed;

        public void Dispose()
        {
            BeginAnimation(AnimationFrameIndexProperty, null);
            Source = null;
            _animation = null;

            _disposed = true;

            Task.Delay(500).ContinueWith(t => ProcessHelper.PerformAggressiveGC());
        }

        private static void LoadFullImage(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
        {
            if (!(obj is AnimatedImage instance))
                return;

            instance._animation = LoadFullImageCore((Uri) ev.NewValue);
            instance.BeginAnimation(AnimationFrameIndexProperty, instance._animation.Animator);
        }

        private static AnimationProvider LoadFullImageCore(Uri path)
        {
            byte[] sign;
            using (var reader =
                new BinaryReader(new FileStream(path.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                sign = reader.BaseStream.Length < 4 ? new byte[] {0, 0, 0, 0} : reader.ReadBytes(4);
            }

            AnimationProvider provider = null;

            if (sign[0] == 'G' && sign[1] == 'I' && sign[2] == 'F' && sign[3] == '8')
                provider = new GIFAnimationProvider(path.LocalPath);
            //else if (sign[0] == 0x89 && sign[1] == 'P' && sign[2] == 'N' && sign[3] == 'G')
            //    provider = new APNGAnimationProvider();
            //else
            //    provider = new ImageMagickProvider();

            return provider;
        }

        #region DependencyProperty

        public static readonly DependencyProperty AnimationFrameIndexProperty =
            DependencyProperty.Register("AnimationFrameIndex", typeof(int), typeof(AnimatedImage),
                new UIPropertyMetadata(-1, AnimationFrameIndexChanged));

        public static readonly DependencyProperty AnimationUriProperty =
            DependencyProperty.Register("AnimationUri", typeof(Uri), typeof(AnimatedImage),
                new UIPropertyMetadata(null, AnimationUriChanged));

        public static readonly DependencyProperty MetaProperty =
            DependencyProperty.Register("Meta", typeof(Meta), typeof(AnimatedImage));

        public int AnimationFrameIndex
        {
            get => (int) GetValue(AnimationFrameIndexProperty);
            set => SetValue(AnimationFrameIndexProperty, value);
        }

        public Uri AnimationUri
        {
            get => (Uri) GetValue(AnimationUriProperty);
            set => SetValue(AnimationUriProperty, value);
        }

        public Meta Meta
        {
            private get => (Meta) GetValue(MetaProperty);
            set => SetValue(MetaProperty, value);
        }

        private static void AnimationUriChanged(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
        {
            if (!(obj is AnimatedImage instance))
                return;

            //var thumbnail = instance.Meta?.GetThumbnail(true);
            //instance.Source = thumbnail;

            LoadFullImage(obj, ev);

            instance.AnimationFrameIndex = 0;
        }

        private static void AnimationFrameIndexChanged(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
        {
            if (!(obj is AnimatedImage instance))
                return;

            new Task(() =>
            {
                var image = instance._animation.GetRenderedFrame((int) ev.NewValue);

                instance.Dispatcher.BeginInvoke(
                    new Action(() => { instance.Source = image; }), DispatcherPriority.Loaded);
            }).Start();
        }

        #endregion DependencyProperty
    }
}