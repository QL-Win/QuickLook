using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Wpf.Ui.Violeta.Resources.Localization;

namespace Wpf.Ui.Violeta.Controls;

/// <summary>
/// A pagination control that displays page navigation buttons.
/// CurrentPage is 1-based.
/// </summary>
[TemplatePart(Name = PART_PreviousButton, Type = typeof(RepeatButton))]
[TemplatePart(Name = PART_NextButton, Type = typeof(RepeatButton))]
[TemplatePart(Name = PART_ButtonPanel, Type = typeof(StackPanel))]
[TemplatePart(Name = PART_QuickJumpInput, Type = typeof(TextBox))]
[TemplatePart(Name = PART_PageSizeSelector, Type = typeof(ComboBox))]
public class Pagination : Control
{
    public const string PART_PreviousButton = "PART_PreviousButton";
    public const string PART_NextButton = "PART_NextButton";
    public const string PART_ButtonPanel = "PART_ButtonPanel";
    public const string PART_QuickJumpInput = "PART_QuickJumpInput";
    public const string PART_PageSizeSelector = "PART_PageSizeSelector";

    // 7 internal page buttons (matches Ursa design)
    private readonly PaginationButton[] _buttons = new PaginationButton[7];

    private StackPanel? _buttonPanel;
    private RepeatButton? _previousButton;
    private RepeatButton? _nextButton;
    private TextBox? _quickJumpInput;
    private ComboBox? _pageSizeSelector;

    static Pagination()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Pagination),
            new FrameworkPropertyMetadata(typeof(Pagination)));
    }

    // --- Routed Events ----------------------------------------------------------

    public static readonly RoutedEvent CurrentPageChangedEvent =
        EventManager.RegisterRoutedEvent(
            nameof(CurrentPageChanged),
            RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<int>),
            typeof(Pagination));

    /// <summary>Raised when <see cref="CurrentPage"/> changes.</summary>
    public event RoutedPropertyChangedEventHandler<int> CurrentPageChanged
    {
        add => AddHandler(CurrentPageChangedEvent, value);
        remove => RemoveHandler(CurrentPageChangedEvent, value);
    }

    // --- Dependency Properties ---------------------------------------------------

    public static readonly DependencyProperty CurrentPageProperty =
        DependencyProperty.Register(
            nameof(CurrentPage),
            typeof(int),
            typeof(Pagination),
            new FrameworkPropertyMetadata(
                1,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnCurrentPageChanged,
                CoerceCurrentPage));

    public static readonly DependencyProperty TotalCountProperty =
        DependencyProperty.Register(
            nameof(TotalCount),
            typeof(int),
            typeof(Pagination),
            new PropertyMetadata(0, OnTotalCountOrPageSizeChanged));

    public static readonly DependencyProperty PageSizeProperty =
        DependencyProperty.Register(
            nameof(PageSize),
            typeof(int),
            typeof(Pagination),
            new PropertyMetadata(10, OnTotalCountOrPageSizeChanged));

    public static readonly DependencyProperty PageCountProperty =
        DependencyProperty.Register(
            nameof(PageCount),
            typeof(int),
            typeof(Pagination),
            new PropertyMetadata(0));

    public static readonly DependencyProperty ShowQuickJumpProperty =
        DependencyProperty.Register(
            nameof(ShowQuickJump),
            typeof(bool),
            typeof(Pagination),
            new PropertyMetadata(false));

    public static readonly DependencyProperty QuickJumpPrefixTextProperty =
        DependencyProperty.Register(
            nameof(QuickJumpPrefixText),
            typeof(string),
            typeof(Pagination),
            new PropertyMetadata(SH.PaginationQuickJumpPrefix));

    public static readonly DependencyProperty QuickJumpSuffixTextProperty =
        DependencyProperty.Register(
            nameof(QuickJumpSuffixText),
            typeof(string),
            typeof(Pagination),
            new PropertyMetadata(SH.PaginationQuickJumpSuffix));

    public static readonly DependencyProperty ShowPageSizeSelectorProperty =
        DependencyProperty.Register(
            nameof(ShowPageSizeSelector),
            typeof(bool),
            typeof(Pagination),
            new PropertyMetadata(false));

    public static readonly DependencyProperty PageSizeOptionsProperty =
        DependencyProperty.Register(
            nameof(PageSizeOptions),
            typeof(int[]),
            typeof(Pagination),
            new PropertyMetadata(new int[] { 10, 20, 50, 100 }));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(Pagination),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(Pagination),
            new PropertyMetadata(null));

    // --- Properties -------------------------------------------------------------

    /// <summary>Current 1-based page index.</summary>
    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    /// <summary>Total number of items.</summary>
    public int TotalCount
    {
        get => (int)GetValue(TotalCountProperty);
        set => SetValue(TotalCountProperty, value);
    }

    /// <summary>Number of items per page (default 10).</summary>
    public int PageSize
    {
        get => (int)GetValue(PageSizeProperty);
        set => SetValue(PageSizeProperty, value);
    }

    /// <summary>Computed total number of pages.</summary>
    public int PageCount
    {
        get => (int)GetValue(PageCountProperty);
        private set => SetValue(PageCountProperty, value);
    }

    /// <summary>Whether the quick-jump text box is shown.</summary>
    public bool ShowQuickJump
    {
        get => (bool)GetValue(ShowQuickJumpProperty);
        set => SetValue(ShowQuickJumpProperty, value);
    }

    /// <summary>Localized quick-jump prefix text.</summary>
    public string QuickJumpPrefixText
    {
        get => (string)GetValue(QuickJumpPrefixTextProperty);
        set => SetValue(QuickJumpPrefixTextProperty, value);
    }

    /// <summary>Localized quick-jump suffix text.</summary>
    public string QuickJumpSuffixText
    {
        get => (string)GetValue(QuickJumpSuffixTextProperty);
        set => SetValue(QuickJumpSuffixTextProperty, value);
    }

    /// <summary>Whether the page-size selector ComboBox is shown.</summary>
    public bool ShowPageSizeSelector
    {
        get => (bool)GetValue(ShowPageSizeSelectorProperty);
        set => SetValue(ShowPageSizeSelectorProperty, value);
    }

    /// <summary>Options shown in the page-size selector (default 10/20/50/100).</summary>
    public int[] PageSizeOptions
    {
        get => (int[])GetValue(PageSizeOptionsProperty);
        set => SetValue(PageSizeOptionsProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public Pagination()
    {
        if (ReadLocalValue(QuickJumpPrefixTextProperty) == DependencyProperty.UnsetValue)
        {
            SetCurrentValue(QuickJumpPrefixTextProperty, SH.PaginationQuickJumpPrefix);
        }

        if (ReadLocalValue(QuickJumpSuffixTextProperty) == DependencyProperty.UnsetValue)
        {
            SetCurrentValue(QuickJumpSuffixTextProperty, SH.PaginationQuickJumpSuffix);
        }
    }

    // --- Coerce / Change callbacks -----------------------------------------------

    private static object CoerceCurrentPage(DependencyObject d, object baseValue)
    {
        if (d is Pagination p && baseValue is int value)
        {
            int pageCount = p.PageCount;
            if (pageCount <= 0) return 1;
            return Clamp(value, 1, pageCount);
        }
        return baseValue;
    }

    private static void OnCurrentPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Pagination p) return;
        int oldValue = (int)e.OldValue;
        int newValue = (int)e.NewValue;
        p.UpdateButtons();
        p.RaiseEvent(new RoutedPropertyChangedEventArgs<int>(oldValue, newValue, CurrentPageChangedEvent));
        p.InvokeCommand();
    }

    private static void OnTotalCountOrPageSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Pagination p) p.RecalcPageCount();
    }

    // --- Template ---------------------------------------------------------------

    public override void OnApplyTemplate()
    {
        _previousButton?.Click -= OnPreviousButtonClick;
        _nextButton?.Click -= OnNextButtonClick;
        _pageSizeSelector?.SelectionChanged -= OnPageSizeSelectorChanged;
        if (_quickJumpInput != null)
        {
            _quickJumpInput.KeyDown -= OnQuickJumpKeyDown;
            _quickJumpInput.LostFocus -= OnQuickJumpLostFocus;
        }

        base.OnApplyTemplate();

        _previousButton = GetTemplateChild(PART_PreviousButton) as RepeatButton;
        _nextButton = GetTemplateChild(PART_NextButton) as RepeatButton;
        _buttonPanel = GetTemplateChild(PART_ButtonPanel) as StackPanel;
        _quickJumpInput = GetTemplateChild(PART_QuickJumpInput) as TextBox;
        _pageSizeSelector = GetTemplateChild(PART_PageSizeSelector) as ComboBox;

        _previousButton?.Click += OnPreviousButtonClick;
        _nextButton?.Click += OnNextButtonClick;
        if (_quickJumpInput != null)
        {
            _quickJumpInput.KeyDown += OnQuickJumpKeyDown;
            _quickJumpInput.LostFocus += OnQuickJumpLostFocus;
        }
        _pageSizeSelector?.SelectionChanged += OnPageSizeSelectorChanged;

        InitializePanelButtons();
        RecalcPageCount();
        // Coerce CurrentPage after PageCount is known
        CoerceValue(CurrentPageProperty);
        UpdateButtons();
    }

    // --- Button initialization ---------------------------------------------------

    private void InitializePanelButtons()
    {
        if (_buttonPanel is null) return;
        _buttonPanel.Children.Clear();

        for (int i = 0; i < 7; i++)
        {
            var btn = new PaginationButton { Page = i + 1 };
            btn.Click += OnPageButtonClick;
            _buttonPanel.Children.Add(btn);
            _buttons[i] = btn;
        }
    }

    // --- UpdateButtons -----------------------------------------------------------

    private void UpdateButtons()
    {
        int pageCount = PageCount;
        int currentPage = CurrentPage;

        if (_buttonPanel != null && _buttons[0] != null)
        {
            if (pageCount <= 7)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (i < pageCount)
                    {
                        _buttons[i].Visibility = Visibility.Visible;
                        _buttons[i].SetStatus(i + 1, i + 1 == currentPage, false, false);
                    }
                    else
                    {
                        _buttons[i].Visibility = Visibility.Collapsed;
                    }
                }
            }
            else
            {
                for (int i = 0; i < 7; i++)
                    _buttons[i].Visibility = Visibility.Visible;

                int mid = Clamp(currentPage, 4, pageCount - 3);

                _buttons[0].SetStatus(1, 1 == currentPage, false, false);
                _buttons[6].SetStatus(pageCount, pageCount == currentPage, false, false);

                _buttons[3].SetStatus(mid, mid == currentPage, false, false);
                _buttons[2].SetStatus(mid - 1, mid - 1 == currentPage, false, false);
                _buttons[4].SetStatus(mid + 1, mid + 1 == currentPage, false, false);

                if (mid > 4)
                    _buttons[1].SetStatus(-1, false, true, false); // fast-forward left (…)
                else
                    _buttons[1].SetStatus(mid - 2, mid - 2 == currentPage, false, false);

                if (mid < pageCount - 3)
                    _buttons[5].SetStatus(-1, false, false, true); // fast-backward right (…)
                else
                    _buttons[5].SetStatus(mid + 2, mid + 2 == currentPage, false, false);
            }
        } // end if (_buttonPanel != null && _buttons[0] != null)

        _previousButton?.IsEnabled = currentPage > 1;
        _nextButton?.IsEnabled = currentPage < pageCount;
        RefreshQuickJumpText();
        SyncPageSizeSelector();
    }

    // --- Page count recalculation ------------------------------------------------

    private void RecalcPageCount()
    {
        int ps = PageSize;
        if (ps <= 0) ps = 10;
        int total = TotalCount;
        int count = total / ps + (total % ps > 0 ? 1 : 0);
        PageCount = count;
        CoerceValue(CurrentPageProperty);
        UpdateButtons();
    }

    // --- Event handlers ----------------------------------------------------------

    private void OnPreviousButtonClick(object sender, RoutedEventArgs e) => AddCurrentPage(-1);

    private void OnNextButtonClick(object sender, RoutedEventArgs e) => AddCurrentPage(1);

    private void OnPageButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is PaginationButton btn)
        {
            if (btn.IsFastForward)
                AddCurrentPage(-5);
            else if (btn.IsFastBackward)
                AddCurrentPage(5);
            else
                CurrentPage = btn.Page;
        }
    }

    private void OnQuickJumpKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Return)
            SyncQuickJump();
    }

    private void OnQuickJumpLostFocus(object sender, RoutedEventArgs e) => SyncQuickJump();

    private void SyncPageSizeSelector()
    {
        if (_pageSizeSelector is null) return;
        if (_pageSizeSelector.SelectedItem is int selected && selected == PageSize) return;
        _pageSizeSelector.SelectedItem = PageSize;
    }

    private void OnPageSizeSelectorChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_pageSizeSelector?.SelectedItem is int newSize && newSize > 0)
        {
            PageSize = newSize;
        }
    }

    private void RefreshQuickJumpText()
    {
        if (_quickJumpInput is null) return;
        _quickJumpInput.Text = CurrentPage.ToString();
    }

    private void SyncQuickJump()
    {
        if (_quickJumpInput is null) return;
        if (int.TryParse(_quickJumpInput.Text, out int value))
        {
            CurrentPage = Clamp(value, 1, PageCount);
        }
        RefreshQuickJumpText();
    }

    // --- Helpers -----------------------------------------------------------------

    private void AddCurrentPage(int delta)
    {
        CurrentPage = Clamp(CurrentPage + delta, 1, PageCount);
    }

    private static int Clamp(int value, int min, int max)
        => value < min ? min : value > max ? max : value;

    private void InvokeCommand()
    {
        if (Command is { } cmd && cmd.CanExecute(CommandParameter))
            cmd.Execute(CommandParameter);
    }
}
