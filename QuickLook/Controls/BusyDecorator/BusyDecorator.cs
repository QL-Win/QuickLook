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
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace QuickLook.Controls.BusyDecorator;

[StyleTypedProperty(Property = "BusyStyle", StyleTargetType = typeof(Control))]
public class BusyDecorator : Decorator, IDisposable
{
    private readonly BackgroundVisualHost _busyHost = new BackgroundVisualHost();

    private readonly ResourceDictionary _styles = new ResourceDictionary
    {
        Source = new Uri("pack://application:,,,/QuickLook;component/Controls/BusyDecorator/BusyDecorator.xaml")
    };

    static BusyDecorator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(BusyDecorator),
            new FrameworkPropertyMetadata(typeof(BusyDecorator)));
    }

    public BusyDecorator()
    {
        Resources.MergedDictionaries.Add(_styles);

        AddLogicalChild(_busyHost);
        AddVisualChild(_busyHost);

        SetBinding(_busyHost, IsBusyIndicatorShowingProperty, BackgroundVisualHost.IsContentShowingProperty);
        SetBinding(_busyHost, BusyHorizontalAlignmentProperty, HorizontalAlignmentProperty);
        SetBinding(_busyHost, BusyVerticalAlignmentProperty, VerticalAlignmentProperty);
    }

    protected override int VisualChildrenCount => Child != null ? 2 : 1;

    protected override IEnumerator LogicalChildren
    {
        get
        {
            if (Child != null)
                yield return Child;

            yield return _busyHost;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _busyHost.ThreadedHelper?.Exit();
    }

    protected override Visual GetVisualChild(int index)
    {
        if (Child != null)
            switch (index)
            {
                case 0:
                    return Child;

                case 1:
                    return _busyHost;
            }
        else if (index == 0)
            return _busyHost;

        throw new IndexOutOfRangeException("index");
    }

    private void SetBinding(DependencyObject obj, DependencyProperty source, DependencyProperty target)
    {
        var b = new Binding
        {
            Source = this,
            Path = new PropertyPath(source)
        };
        BindingOperations.SetBinding(obj, target, b);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var ret = new Size(0, 0);
        if (Child != null)
        {
            Child.Measure(constraint);
            ret = Child.DesiredSize;
        }

        _busyHost.Measure(constraint);

        return new Size(Math.Max(ret.Width, _busyHost.DesiredSize.Width),
            Math.Max(ret.Height, _busyHost.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        var ret = new Size(0, 0);
        if (Child != null)
        {
            Child.Arrange(new Rect(arrangeSize));
            ret = Child.RenderSize;
        }

        _busyHost.Arrange(new Rect(arrangeSize));

        return new Size(Math.Max(ret.Width, _busyHost.RenderSize.Width),
            Math.Max(ret.Height, _busyHost.RenderSize.Height));
    }

    #region IsBusyIndicatorShowing Property

    /// <summary>
    ///     Identifies the IsBusyIndicatorShowing dependency property.
    /// </summary>
    public static readonly DependencyProperty IsBusyIndicatorShowingProperty = DependencyProperty.Register(
        "IsBusyIndicatorShowing",
        typeof(bool),
        typeof(BusyDecorator),
        new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>
    ///     Gets or sets if the BusyIndicator is being shown.
    /// </summary>
    public bool IsBusyIndicatorShowing
    {
        get => (bool)GetValue(IsBusyIndicatorShowingProperty);
        set => SetValue(IsBusyIndicatorShowingProperty, value);
    }

    #endregion IsBusyIndicatorShowing Property

    #region BusyStyle

    /// <summary>
    ///     Identifies the <see cref="BusyStyle" /> property.
    /// </summary>
    public static readonly DependencyProperty BusyStyleProperty =
        DependencyProperty.Register(
            "BusyStyle",
            typeof(Style),
            typeof(BusyDecorator),
            new FrameworkPropertyMetadata(OnBusyStyleChanged));

    /// <summary>
    ///     Gets or sets the Style to apply to the Control that is displayed as the busy indication.
    /// </summary>
    public Style BusyStyle
    {
        get => (Style)GetValue(BusyStyleProperty);
        set => SetValue(BusyStyleProperty, value);
    }

    private static void OnBusyStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var bd = (BusyDecorator)d;
        var nVal = (Style)e.NewValue;
        bd._busyHost.CreateContent = () => new Control { Style = nVal };
    }

    #endregion BusyStyle

    #region BusyHorizontalAlignment

    /// <summary>
    ///     Identifies the <see cref="BusyHorizontalAlignment" /> property.
    /// </summary>
    public static readonly DependencyProperty BusyHorizontalAlignmentProperty = DependencyProperty.Register(
        "BusyHorizontalAlignment",
        typeof(HorizontalAlignment),
        typeof(BusyDecorator),
        new FrameworkPropertyMetadata(HorizontalAlignment.Center));

    /// <summary>
    ///     Gets or sets the HorizontalAlignment to use to layout the control that contains the busy indicator control.
    /// </summary>
    public HorizontalAlignment BusyHorizontalAlignment
    {
        get => (HorizontalAlignment)GetValue(BusyHorizontalAlignmentProperty);
        set => SetValue(BusyHorizontalAlignmentProperty, value);
    }

    #endregion BusyHorizontalAlignment

    #region BusyVerticalAlignment

    /// <summary>
    ///     Identifies the <see cref="BusyVerticalAlignment" /> property.
    /// </summary>
    public static readonly DependencyProperty BusyVerticalAlignmentProperty = DependencyProperty.Register(
        "BusyVerticalAlignment",
        typeof(VerticalAlignment),
        typeof(BusyDecorator),
        new FrameworkPropertyMetadata(VerticalAlignment.Center));

    /// <summary>
    ///     Gets or sets the the VerticalAlignment to use to layout the control that contains the busy indicator.
    /// </summary>
    public VerticalAlignment BusyVerticalAlignment
    {
        get => (VerticalAlignment)GetValue(BusyVerticalAlignmentProperty);
        set => SetValue(BusyVerticalAlignmentProperty, value);
    }

    #endregion BusyVerticalAlignment
}
