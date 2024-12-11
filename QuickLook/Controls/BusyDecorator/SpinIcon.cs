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

using System.Windows;
using System.Windows.Controls;

namespace QuickLook.Controls.BusyDecorator;

internal class SpinIcon : TextBlock, ISpinable
{
    #region public bool Spin

    /// <summary>
    ///     Identifies the Spin dependency property.
    /// </summary>
    public static DependencyProperty SpinProperty =
        DependencyProperty.Register("Spin", typeof(bool), typeof(SpinIcon),
            new PropertyMetadata(false, OnSpinPropertyChanged));

    private static void OnSpinPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var b = d as SpinIcon;

        if (b == null) return;

        if ((bool)e.NewValue)
            b.BeginSpin();
        else
            b.StopSpin();
    }

    /// <summary>
    ///     Gets or sets the current spin (angle) animation of the icon.
    /// </summary>
    public bool Spin
    {
        get => (bool)GetValue(SpinProperty);

        set => SetValue(SpinProperty, value);
    }

    #endregion public bool Spin

    #region public double SpinDuration

    /// <summary>
    ///     Identifies the SpinDuration dependency property.
    /// </summary>
    public static DependencyProperty SpinDurationProperty =
        DependencyProperty.Register("SpinDuration", typeof(double), typeof(SpinIcon),
            new PropertyMetadata(1d, SpinDurationChanged));

    private static void SpinDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var b = d as SpinIcon;

        if (null == b || !b.Spin || !(e.NewValue is double) ||
            e.NewValue.Equals(e.OldValue)) return;

        b.StopSpin();
        b.BeginSpin();
    }

    /// <summary>
    ///     Gets or sets the duration of the spinning animation (in seconds). This will stop and start the spin animation.
    /// </summary>
    public double SpinDuration
    {
        get => (double)GetValue(SpinDurationProperty);

        set => SetValue(SpinDurationProperty, value);
    }

    #endregion public double SpinDuration
}
