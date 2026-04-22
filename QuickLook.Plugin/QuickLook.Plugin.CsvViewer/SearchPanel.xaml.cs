using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickLook.Plugin.CsvViewer;

public partial class SearchPanel : UserControl
{
    public event EventHandler<string> SearchTextChanged;

    public event EventHandler<bool> MatchCaseChanged;

    public event EventHandler FindNextRequested;

    public event EventHandler FindPreviousRequested;

    public event EventHandler CloseRequested;

    public SearchPanel()
    {
        InitializeComponent();
    }

    public string SearchText
    {
        get => searchTextBox.Text;
        set => searchTextBox.Text = value;
    }

    public bool MatchCase => matchCaseToggle.IsChecked == true;

    public void FocusSearchText()
    {
        searchTextBox.Focus();
        searchTextBox.SelectAll();
    }

    public void SetMatchCount(int current, int total)
    {
        matchCountTextBlock.Text = total == 0 ? "No matches" : $"{current} of {total}";
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchTextChanged?.Invoke(this, searchTextBox.Text);
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                FindPreviousRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                FindNextRequested?.Invoke(this, EventArgs.Empty);
            }

            e.Handled = true;
        }
    }

    private void MatchCaseToggle_Checked(object sender, RoutedEventArgs e)
    {
        MatchCaseChanged?.Invoke(this, MatchCase);
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        FindPreviousRequested?.Invoke(this, EventArgs.Empty);
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        FindNextRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
