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

using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace QuickLook.Controls;

public sealed class LeftContextMenuBehavior : Behavior<FrameworkElement>
{
    public Point? PlacementOffset { get; set; } = null;
    public PlacementMode Placement { get; set; } = PlacementMode.Bottom;

    public double? PlacementOffsetX
    {
        get => PlacementOffset?.X;
        set => PlacementOffset = value != null ? new(value ?? 0d, PlacementOffset?.Y ?? 0d) : null;
    }

    public double? PlacementOffsetY
    {
        get => PlacementOffset?.Y;
        set => PlacementOffset = value != null ? new(PlacementOffset?.X ?? 0d, value ?? 0d) : null;
    }

    public LeftContextMenuBehavior()
    {
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        Register(AssociatedObject, PlacementOffset, Placement);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        Unregister(AssociatedObject);
    }

    public static void Register(FrameworkElement frameworkElement, Point? placementOffset = null, PlacementMode placement = PlacementMode.Bottom)
    {
        if (frameworkElement?.ContextMenu == null)
        {
            return;
        }
        frameworkElement.PreviewMouseRightButtonUp += (_, e) => e.Handled = true;
        frameworkElement.MouseRightButtonUp += (_, e) => e.Handled = true;
        frameworkElement.PreviewMouseLeftButtonDown += (_, _) =>
        {
            ContextMenu contextMenu = frameworkElement.ContextMenu;

            if (contextMenu != null)
            {
                if (contextMenu.PlacementTarget != frameworkElement)
                {
                    contextMenu.PlacementTarget = frameworkElement;
                    contextMenu.PlacementRectangle = new Rect(placementOffset ?? new Point(), new Size(frameworkElement.ActualWidth, frameworkElement.ActualHeight));
                    contextMenu.Placement = placement;
                    contextMenu.StaysOpen = false;
                }
                contextMenu.IsOpen = !contextMenu.IsOpen;
            }
        };
    }

    public static void Unregister(FrameworkElement frameworkElement)
    {
        _ = frameworkElement;
    }
}
