using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuickLook.Plugin.PEViewer;

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
