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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuickLook.Plugin.ELFViewer.InfoPanels;

public class FluentBorder : Decorator
{
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(FluentBorder), new FrameworkPropertyMetadata(new CornerRadius()));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register("Background", typeof(Brush), typeof(FluentBorder), new FrameworkPropertyMetadata(Brushes.Transparent));

    public Brush Background
    {
        get => (Brush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (Child != null)
        {
            Rect rect = new(new Point(0, 0), RenderSize);
            drawingContext.DrawRoundedRectangle(Background, new Pen(Brushes.Transparent, 1), rect, CornerRadius.TopLeft, CornerRadius.TopRight);
        }
    }
}
