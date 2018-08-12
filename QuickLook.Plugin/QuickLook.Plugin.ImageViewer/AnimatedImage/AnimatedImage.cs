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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    public class AnimatedImage : Image, IDisposable
    {
        private AnimationProvider _animation;
        private bool _disposing;

        public void Dispose()
        {
            _disposing = true;

            BeginAnimation(AnimationFrameIndexProperty, null);
            Source = null;

            _animation?.Dispose();
            _animation = null;
        }

        private static AnimationProvider LoadFullImageCore(Uri path, NConvert meta, Dispatcher uiDispatcher)
        {
            byte[] sign;
            using (var reader =
                new BinaryReader(new FileStream(path.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                sign = reader.BaseStream.Length < 4 ? new byte[] {0, 0, 0, 0} : reader.ReadBytes(4);
            }

            AnimationProvider provider;

            if (sign[0] == 'G' && sign[1] == 'I' && sign[2] == 'F' && sign[3] == '8')
                provider = new GifAnimationProvider(path.LocalPath, meta, uiDispatcher);
            else if (sign[0] == 0x89 && sign[1] == 'P' && sign[2] == 'N' && sign[3] == 'G')
                provider = new APNGAnimationProvider(path.LocalPath, meta, uiDispatcher);
            else
                provider = new ImageMagickProvider(path.LocalPath, meta, uiDispatcher);

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
            DependencyProperty.Register("Meta", typeof(NConvert), typeof(AnimatedImage));

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

        public NConvert Meta
        {
            private get => (NConvert) GetValue(MetaProperty);
            set => SetValue(MetaProperty, value);
        }

        private static void AnimationUriChanged(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
        {
            if (!(obj is AnimatedImage instance))
                return;

            //var thumbnail = instance.Meta?.GetThumbnail(true);
            //instance.Source = thumbnail;

            instance._animation = LoadFullImageCore((Uri) ev.NewValue, instance.Meta, instance.Dispatcher);

            instance.BeginAnimation(AnimationFrameIndexProperty, instance._animation.Animator);
            instance.AnimationFrameIndex = 0;
        }

        private static void AnimationFrameIndexChanged(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
        {
            if (!(obj is AnimatedImage instance))
                return;

            if (instance._disposing)
                return;

            var task = instance._animation.GetRenderedFrame((int) ev.NewValue);

            if (instance.Source == null && (int) ev.NewValue == 0) // this is the first image. Run it synchronously.
            {
                task.Start();
                task.Wait(5000);
            }

            if (task.IsCompleted)
            {
                instance.Source = task.Result;
                return;
            }

            task.ContinueWith(t => { instance.Dispatcher.Invoke(() => instance.Source = t.Result); });
            task.Start();
        }

        #endregion DependencyProperty
    }
}