using QuickLook.Common.Helpers;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickLook.Plugin.CsvViewer;

public partial class SearchPanel : UserControl
{
    private readonly string _translationFile;

    public event EventHandler<string> SearchTextChanged;

    public event EventHandler<bool> MatchCaseChanged;

    public event EventHandler FindNextRequested;

    public event EventHandler FindPreviousRequested;

    public event EventHandler CloseRequested;

    public SearchPanel()
    {
        InitializeComponent();

        _translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");

        searchTextBox.ToolTip = TranslationHelper.Get("Search_Placeholder", _translationFile, failsafe: "Search...");
        matchCaseToggle.Content = TranslationHelper.Get("Search_MatchCase", _translationFile, failsafe: "Match case");
        previousButton.ToolTip = TranslationHelper.Get("Search_PreviousMatch", _translationFile, failsafe: "Previous match");
        nextButton.ToolTip = TranslationHelper.Get("Search_NextMatch", _translationFile, failsafe: "Next match");
        matchCountTextBlock.Text = TranslationHelper.Get("Search_NoMatches", _translationFile, failsafe: "No matches");
        closeButton.ToolTip = TranslationHelper.Get("Search_Close", _translationFile, failsafe: "Close");
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
        if (total == 0)
        {
            matchCountTextBlock.Text = TranslationHelper.Get("Search_NoMatches", _translationFile, failsafe: "No matches");
        }
        else
        {
            var fmt = TranslationHelper.Get("Search_MatchCount", _translationFile, failsafe: "{0} of {1}");
            matchCountTextBlock.Text = string.Format(CultureInfo.CurrentCulture, fmt, current, total);
        }
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
