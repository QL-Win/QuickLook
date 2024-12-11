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
using System.Windows.Data;
using System.Windows.Media;
using QuickLook.Common.Helpers;

namespace QuickLook.Controls.GlassLayer;

/// <summary>
///     Interaction logic for GlassLayer.xaml
/// </summary>
public partial class GlassLayer : UserControl
{
    public GlassLayer()
    {
        InitializeComponent();

        var scale = DisplayDeviceHelper.GetCurrentScaleFactor();
        noiseBrush.Viewport = new Rect(new Size(
            noiseBrush.ImageSource.Width / scale.Horizontal,
            noiseBrush.ImageSource.Height / scale.Vertical));

        LayoutUpdated += (sender, e) =>
        {
            BindingOperations.GetBindingExpressionBase(brush, TileBrush.ViewboxProperty)?.UpdateTarget();
        };
    }

    #region public Visual BlurredElement

    /// <summary>
    ///     Identifies the BlurredElement dependency property.
    /// </summary>
    public static DependencyProperty BlurredElementProperty =
        DependencyProperty.Register("BlurredElement", typeof(Visual), typeof(GlassLayer), null);

    /// <summary>
    /// </summary>
    public Visual BlurredElement
    {
        get => (Visual)GetValue(BlurredElementProperty);

        set => SetValue(BlurredElementProperty, value);
    }

    #endregion public Visual BlurredElement

    #region public SolidColorBrush OverlayColor

    /// <summary>
    ///     Identifies the OverlayColor dependency property.
    /// </summary>
    public static DependencyProperty OverlayColorProperty =
        DependencyProperty.Register("OverlayColor", typeof(SolidColorBrush), typeof(GlassLayer),
            new UIPropertyMetadata(new SolidColorBrush(Color.FromRgb(0xDA, 0xDA, 0xDA))));

    /// <summary>
    /// </summary>
    public SolidColorBrush OverlayColor
    {
        get => (SolidColorBrush)GetValue(OverlayColorProperty);

        set => SetValue(OverlayColorProperty, value);
    }

    #endregion public SolidColorBrush OverlayColor

    #region public Visibility ColorOverlayVisibility

    /// <summary>
    ///     Identifies the ColorOverlayVisibilityProperty dependency property.
    /// </summary>
    public static DependencyProperty ColorOverlayVisibilityProperty =
        DependencyProperty.Register("ColorOverlayVisibility", typeof(Visibility), typeof(GlassLayer),
            new UIPropertyMetadata(Visibility.Visible));

    public Visibility ColorOverlayVisibility
    {
        get => (Visibility)GetValue(ColorOverlayVisibilityProperty);

        set => SetValue(ColorOverlayVisibilityProperty, value);
    }

    #endregion public Visibility ColorOverlayVisibility

    #region public Visibility NoiseVisibility

    /// <summary>
    ///     Identifies the NoiseVisibility dependency property.
    /// </summary>
    public static DependencyProperty NoiseVisibilityProperty =
        DependencyProperty.Register("NoiseVisibility", typeof(Visibility), typeof(GlassLayer),
            new UIPropertyMetadata(Visibility.Visible));

    /// <summary>
    /// </summary>
    public Visibility NoiseVisibility
    {
        get => (Visibility)GetValue(NoiseVisibilityProperty);

        set => SetValue(NoiseVisibilityProperty, value);
    }

    #endregion public Visibility NoiseVisibility

    #region public Visibility GlassVisibility

    /// <summary>
    ///     Identifies the GlassVisibility dependency property.
    /// </summary>
    public static DependencyProperty GlassVisibilityProperty =
        DependencyProperty.Register("GlassVisibility", typeof(Visibility), typeof(GlassLayer),
            new UIPropertyMetadata(Visibility.Visible));

    /// <summary>
    /// </summary>
    public Visibility GlassVisibility
    {
        get => (Visibility)GetValue(GlassVisibilityProperty);

        set => SetValue(GlassVisibilityProperty, value);
    }

    #endregion public Visibility GlassVisibility
}
