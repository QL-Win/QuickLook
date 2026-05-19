using System.Windows;
using System.Windows.Controls.Primitives;

namespace Wpf.Ui.Violeta.Controls;

/// <summary>
/// A button used inside a <see cref="Pagination"/> control to represent a single page.
/// </summary>
public class PaginationButton : RepeatButton
{
    public static readonly DependencyProperty PageProperty =
        DependencyProperty.Register(nameof(Page), typeof(int), typeof(PaginationButton),
            new PropertyMetadata(0));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(PaginationButton),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsFastForwardProperty =
        DependencyProperty.Register(nameof(IsFastForward), typeof(bool), typeof(PaginationButton),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsFastBackwardProperty =
        DependencyProperty.Register(nameof(IsFastBackward), typeof(bool), typeof(PaginationButton),
            new PropertyMetadata(false));

    /// <summary>Gets or sets the page number this button represents.</summary>
    public int Page
    {
        get => (int)GetValue(PageProperty);
        set => SetValue(PageProperty, value);
    }

    /// <summary>Gets or sets whether this button represents the currently selected page.</summary>
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// Gets or sets whether this button is the left ellipsis (skip backward 5 pages).
    /// </summary>
    public bool IsFastForward
    {
        get => (bool)GetValue(IsFastForwardProperty);
        set => SetValue(IsFastForwardProperty, value);
    }

    /// <summary>
    /// Gets or sets whether this button is the right ellipsis (skip forward 5 pages).
    /// </summary>
    public bool IsFastBackward
    {
        get => (bool)GetValue(IsFastBackwardProperty);
        set => SetValue(IsFastBackwardProperty, value);
    }

    internal void SetStatus(int page, bool isSelected, bool isFastForward, bool isFastBackward)
    {
        Page = page;
        IsSelected = isSelected;
        IsFastForward = isFastForward;
        IsFastBackward = isFastBackward;
    }

    internal void SetSelected(bool isSelected)
    {
        IsSelected = isSelected;
    }

    static PaginationButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PaginationButton),
            new FrameworkPropertyMetadata(typeof(PaginationButton)));
    }
}
