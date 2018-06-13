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
using System.Drawing;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using QuickLook.Common.ExtensionMethods;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage
{
    internal class GifAnimationProvider : AnimationProvider
    {
        private Bitmap _frame;
        private BitmapSource _frameSource;
        private bool _isPlaying;

        public GifAnimationProvider(string path, Dispatcher uiDispatcher) : base(path, uiDispatcher)
        {
            _frame = (Bitmap) Image.FromFile(path);
            _frameSource = _frame.ToBitmapSource();

            Animator = new Int32Animation(0, 1, new Duration(TimeSpan.FromMilliseconds(50)))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
        }

        public override void Dispose()
        {
            if (_frame == null)
                return;

            ImageAnimator.StopAnimate(_frame, OnFrameChanged);
            _frame.Dispose();

            _frame = null;
            _frameSource = null;
        }

        public override ImageSource GetRenderedFrame(int index)
        {
            if (!_isPlaying)
            {
                _isPlaying = true;
                ImageAnimator.Animate(_frame, OnFrameChanged);
            }

            return _frameSource;
        }

        private void OnFrameChanged(object sender, EventArgs e)
        {
            ImageAnimator.UpdateFrames();
            _frameSource = _frame.ToBitmapSource();
        }
    }
}
