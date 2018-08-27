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

using QuickLook.Common.Annotations;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    public class AnimatedImage : Image, IDisposable
    {
        // List<Pair<formats, type>>
        public static List<KeyValuePair<string[], Type>> Providers = new List<KeyValuePair<string[], Type>>();

        private AnimationProvider _animation;
        private bool _disposing;

        public event EventHandler ImageLoaded;
        public event EventHandler DoZoomToFit;

        public void Dispose()
        {
            _disposing = true;

            BeginAnimation(AnimationFrameIndexProperty, null);
            Source = null;

            _animation?.Dispose();
            _animation = null;
        }

        private static AnimationProvider LoadFullImageCore(Uri path)
        {
            var ext = Path.GetExtension(path.LocalPath).ToLower();
            var type = Providers.First(p => p.Key.Contains(ext) || p.Key.Contains("*")).Value;

            var provider = type.CreateInstance<AnimationProvider>(path.LocalPath);

            return provider;
        }

        #region DependencyProperty

        public static readonly DependencyProperty AnimationFrameIndexProperty =
            DependencyProperty.Register("AnimationFrameIndex", typeof(int), typeof(AnimatedImage),
                new UIPropertyMetadata(-2, AnimationFrameIndexChanged));

        public static readonly DependencyProperty AnimationUriProperty =
            DependencyProperty.Register("AnimationUri", typeof(Uri), typeof(AnimatedImage),
                new UIPropertyMetadata(null, AnimationUriChanged));

        public static readonly DependencyProperty MetaProperty =
            DependencyProperty.Register("Meta", typeof(NConvert), typeof(AnimatedImage));

        public static readonly DependencyProperty ContextObjectProperty =
            DependencyProperty.Register("ContextObject", typeof(ContextObject), typeof(AnimatedImage));

        public int AnimationFrameIndex
        {
            get => (int)GetValue(AnimationFrameIndexProperty);
            set => SetValue(AnimationFrameIndexProperty, value);
        }

        public Uri AnimationUri
        {
            get => (Uri)GetValue(AnimationUriProperty);
            set => SetValue(AnimationUriProperty, value);
        }

        public NConvert Meta
        {
            private get => (NConvert)GetValue(MetaProperty);
            set => SetValue(MetaProperty, value);
        }

        public ContextObject ContextObject
        {
            private get => (ContextObject)GetValue(ContextObjectProperty);
            set => SetValue(ContextObjectProperty, value);
        }

        private static void AnimationUriChanged(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
        {
            if (!(obj is AnimatedImage instance))
                return;

            //var thumbnail = instance.Meta?.GetThumbnail(true);
            //instance.Source = thumbnail;

            instance._animation = LoadFullImageCore((Uri)ev.NewValue);

            instance.BeginAnimation(AnimationFrameIndexProperty, instance._animation.Animator);
            instance.AnimationFrameIndex = -1;
        }

        private static void AnimationFrameIndexChanged(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
        {
            if (!(obj is AnimatedImage instance))
                return;

            if (instance._disposing)
                return;


            if ((int)ev.NewValue == -1) // get thumbnail
            {
                var task = instance._animation.GetThumbnail(instance.ContextObject.PreferredSize, instance.Meta.GetSize());

                if (task != null)
                {
                    task.ContinueWith(_ => instance.Dispatcher.Invoke(() =>
                    {
                        instance.Source = _.Result;
                        instance.DoZoomToFit?.Invoke(instance, new EventArgs());
                        instance.ImageLoaded?.Invoke(instance, new EventArgs());
                    }));
                    task.Start();
                }
            }
            else // begin to loop in the animator
            {
                var task = instance._animation.GetRenderedFrame((int)ev.NewValue);

                task.ContinueWith(_ => instance.Dispatcher.Invoke(() =>
                {
                    instance.Source = _.Result;
                }));
                task.Start();
            }
        }

        #endregion DependencyProperty
    }
}