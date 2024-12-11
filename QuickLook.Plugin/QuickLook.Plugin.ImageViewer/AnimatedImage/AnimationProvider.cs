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

using QuickLook.Common.Plugin;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ImageViewer.AnimatedImage;

internal abstract class AnimationProvider : IDisposable
{
    protected AnimationProvider(Uri path, MetaProvider meta, ContextObject contextObject)
    {
        Path = path;
        Meta = meta;
        ContextObject = contextObject;
    }

    public Uri Path { get; }

    public MetaProvider Meta { get; }

    public ContextObject ContextObject { get; }

    public Int32AnimationUsingKeyFrames Animator { get; protected set; }

    public abstract void Dispose();

    public abstract Task<BitmapSource> GetThumbnail(Size renderSize);

    public abstract Task<BitmapSource> GetRenderedFrame(int index);
}
