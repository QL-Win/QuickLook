// Copyright © 2017-2025 QL-Win Contributors
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

using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage.Providers;

internal class WmpProvider : AnimationProvider
{
    public WmpProvider(Uri path, MetaProvider meta, ContextObject contextObject) : base(path, meta, contextObject)
    {
        Animator = new Int32AnimationUsingKeyFrames();
        Animator.KeyFrames.Add(new DiscreteInt32KeyFrame(0,
            KeyTime.FromTimeSpan(TimeSpan.Zero)));
    }

    public override Task<BitmapSource> GetThumbnail(Size renderSize)
    {
        // Skip thumbnail
        return new Task<BitmapSource>(() => null);
    }

    public override Task<BitmapSource> GetRenderedFrame(int index)
    {
        return new Task<BitmapSource>(() =>
        {
            try
            {
                BitmapDecoder decoder = new WmpBitmapDecoder(
                    Path,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

                return decoder.Frames[0];
            }
            catch (Exception e)
            {
                ProcessHelper.WriteLog(e.ToString());
                return null;
            }
        });
    }

    public override void Dispose()
    {
    }
}
