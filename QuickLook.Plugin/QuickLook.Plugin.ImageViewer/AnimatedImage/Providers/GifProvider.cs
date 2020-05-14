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
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using Size = System.Windows.Size;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers
{
    internal class GifProvider : AnimationProvider
    {
        private Bitmap _fileHandle;
        private BitmapSource _frame;
        private bool _isPlaying;

        public GifProvider(string path, MetaProvider meta) : base(path, meta)
        {
            _fileHandle = (Bitmap) Image.FromFile(path);

            _fileHandle.SetResolution(DpiHelper.DefaultDpi * DpiHelper.GetCurrentScaleFactor().Horizontal,
                DpiHelper.DefaultDpi * DpiHelper.GetCurrentScaleFactor().Vertical);

            Animator = new Int32AnimationUsingKeyFrames {RepeatBehavior = RepeatBehavior.Forever};
            Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(10))));
            Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(20))));
        }

        public override void Dispose()
        {
            if (_fileHandle == null)
                return;

            ImageAnimator.StopAnimate(_fileHandle, OnFrameChanged);
            _fileHandle.Dispose();

            _fileHandle = null;
            _frame = null;
        }

        public override Task<BitmapSource> GetThumbnail(Size renderSize)
        {
            return new Task<BitmapSource>(() =>
            {
                _frame = _fileHandle.ToBitmapSource();
                return _frame;
            });
        }

        public override Task<BitmapSource> GetRenderedFrame(int index)
        {
            return new Task<BitmapSource>(() =>
            {
                if (!_isPlaying)
                {
                    _isPlaying = true;
                    ImageAnimator.Animate(_fileHandle, OnFrameChanged);
                }

                return _frame;
            });
        }

        private void OnFrameChanged(object sender, EventArgs e)
        {
            ImageAnimator.UpdateFrames();
            _frame = _fileHandle.ToBitmapSource();
        }
    }
}