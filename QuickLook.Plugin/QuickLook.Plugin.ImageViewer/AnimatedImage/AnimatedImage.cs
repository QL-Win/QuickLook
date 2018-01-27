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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using QuickLook.Common.Helpers;
using QuickLook.Plugin.ImageViewer.Exiv2;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    public class AnimatedImage : Image, IDisposable
    {
        public static readonly DependencyProperty AnimationUriProperty =
            DependencyProperty.Register("AnimationUri", typeof(Uri), typeof(AnimatedImage),
                new UIPropertyMetadata(null, LoadImage));

        public static readonly DependencyProperty MetaProperty =
            DependencyProperty.Register("Meta", typeof(Meta), typeof(AnimatedImage));

        private ObjectAnimationUsingKeyFrames _animator = new ObjectAnimationUsingKeyFrames();
        private bool _disposed;

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

        public void Dispose()
        {
            BeginAnimation(SourceProperty, null);
            Source = null;
            _animator.KeyFrames.Clear();

            _disposed = true;
        }

        private static void LoadImage(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
        {
            var instance = obj as AnimatedImage;
            if (instance == null)
                return;

            var thumbnail = instance.Meta?.GetThumbnail(true);
            instance.Source = thumbnail;

            if (thumbnail != null)
                LoadFullImageAsync(obj, ev);
            else
                LoadFullImage(obj, ev);
        }

        private static void LoadFullImage(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
        {
            var instance = obj as AnimatedImage;
            if (instance == null)
                return;

            instance._animator = LoadFullImageCore((Uri) ev.NewValue);
            instance.BeginAnimation(SourceProperty, instance._animator);
        }

        private static void LoadFullImageAsync(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
        {
            Task.Run(() =>
            {
                var instance = obj as AnimatedImage;
                if (instance == null)
                    return;

                var animator = LoadFullImageCore((Uri) ev.NewValue);

                instance.Dispatcher.Invoke(DispatcherPriority.Render,
                    new Action(() =>
                    {
                        if (instance._disposed)
                        {
                            ProcessHelper.PerformAggressiveGC();
                            return;
                        }

                        instance._animator = animator;
                        instance.BeginAnimation(SourceProperty, instance._animator);

                        Debug.WriteLine($"LoadFullImageAsync {Thread.CurrentThread.ManagedThreadId}");
                    }));
            });
        }

        private static ObjectAnimationUsingKeyFrames LoadFullImageCore(Uri path)
        {
            byte[] sign;
            using (var reader =
                new BinaryReader(new FileStream(path.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                sign = reader.BaseStream.Length < 4 ? new byte[] {0, 0, 0, 0} : reader.ReadBytes(4);
            }

            IAnimationProvider provider;

            if (sign[0] == 'G' && sign[1] == 'I' && sign[2] == 'F' && sign[3] == '8')
                provider = new GIFAnimationProvider();
            else if (sign[0] == 0x89 && sign[1] == 'P' && sign[2] == 'N' && sign[3] == 'G')
                provider = new APNGAnimationProvider();
            else
                provider = new ImageMagickProvider();

            var animator = new ObjectAnimationUsingKeyFrames();
            provider.GetAnimator(animator, path.LocalPath);
            animator.Freeze();

            return animator;
        }
    }
}