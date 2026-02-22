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

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickLook.Plugin.CsvViewer.Controls;

public partial class SearchPanel : UserControl
{
    public event EventHandler<SearchEventArgs> SearchRequested;
    public event EventHandler<NavigateEventArgs> NavigateRequested;
    public event EventHandler CloseRequested;

    public SearchPanel()
    {
        InitializeComponent();
    }

    public string SearchText => searchTextBox.Text;
    public bool MatchCase => matchCaseCheckBox.IsChecked == true;

    public new void Focus()
    {
        searchTextBox.Focus();
        searchTextBox.SelectAll();
    }

    public void UpdateMatchCount(int totalCount, int currentIndex)
    {
        if (totalCount == 0)
        {
            matchCountText.Text = string.IsNullOrEmpty(searchTextBox.Text) ? "" : "0/0";
        }
        else
        {
            matchCountText.Text = $"{currentIndex + 1}/{totalCount}";
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchRequested?.Invoke(this, new SearchEventArgs(searchTextBox.Text, MatchCase));
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                FindPrevious_Click(sender, e);
            }
            else
            {
                FindNext_Click(sender, e);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CloseSearch_Click(sender, e);
            e.Handled = true;
        }
    }

    private void FindPrevious_Click(object sender, RoutedEventArgs e)
    {
        NavigateRequested?.Invoke(this, new NavigateEventArgs(false));
    }

    private void FindNext_Click(object sender, RoutedEventArgs e)
    {
        NavigateRequested?.Invoke(this, new NavigateEventArgs(true));
    }

    private void MatchCase_Changed(object sender, RoutedEventArgs e)
    {
        SearchRequested?.Invoke(this, new SearchEventArgs(searchTextBox.Text, MatchCase));
    }

    private void CloseSearch_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}

public class SearchEventArgs : EventArgs
{
    public string SearchText { get; }
    public bool MatchCase { get; }

    public SearchEventArgs(string searchText, bool matchCase)
    {
        SearchText = searchText;
        MatchCase = matchCase;
    }
}

public class NavigateEventArgs : EventArgs
{
    public bool Forward { get; }

    public NavigateEventArgs(bool forward)
    {
        Forward = forward;
    }
}
