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
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace QuickLook.Controls.BusyDecorator;

/// <summary>
///     Control extensions
/// </summary>
internal static class ControlExtensions
{
    /// <summary>
    ///     The key used for storing the spinner Storyboard.
    /// </summary>
    private static readonly string SpinnerStoryBoardName = $"{typeof(FrameworkElement).Name}Spinner";

    /// <summary>
    ///     Start the spinning animation
    /// </summary>
    /// <typeparam name="T">FrameworkElement and ISpinable</typeparam>
    /// <param name="control">Control to apply the rotation </param>
    public static void BeginSpin<T>(this T control)
        where T : FrameworkElement, ISpinable
    {
        var transformGroup = control.RenderTransform as TransformGroup ?? new TransformGroup();

        var rotateTransform = transformGroup.Children.OfType<RotateTransform>().FirstOrDefault();

        if (rotateTransform != null)
        {
            rotateTransform.Angle = 0;
        }
        else
        {
            transformGroup.Children.Add(new RotateTransform(0.0));
            control.RenderTransform = transformGroup;
            control.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        var storyboard = new Storyboard();

        var animation = new DoubleAnimation
        {
            From = 0,
            To = 360,
            AutoReverse = false,
            RepeatBehavior = RepeatBehavior.Forever,
            Duration = new Duration(TimeSpan.FromSeconds(control.SpinDuration))
        };
        storyboard.Children.Add(animation);

        Storyboard.SetTarget(animation, control);
        Storyboard.SetTargetProperty(animation,
            new PropertyPath("(0).(1)[0].(2)", UIElement.RenderTransformProperty,
                TransformGroup.ChildrenProperty, RotateTransform.AngleProperty));

        storyboard.Begin();
        control.Resources.Add(SpinnerStoryBoardName, storyboard);
    }

    /// <summary>
    ///     Stop the spinning animation
    /// </summary>
    /// <typeparam name="T">FrameworkElement and ISpinable</typeparam>
    /// <param name="control">Control to stop the rotation.</param>
    public static void StopSpin<T>(this T control)
        where T : FrameworkElement, ISpinable
    {
        var storyboard = control.Resources[SpinnerStoryBoardName] as Storyboard;

        if (storyboard == null) return;

        storyboard.Stop();

        control.Resources.Remove(SpinnerStoryBoardName);
    }
}
