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

using System.Windows;

namespace QuickLook.Plugin.OfficeViewer;

public partial class ProtectedViewDialog : Window
{
    public bool RememberChoice => RememberCheckBox.IsChecked ?? false;
    public bool UserSelectedYes { get; private set; }

    public ProtectedViewDialog()
    {
        InitializeComponent();
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        UserSelectedYes = true;
        DialogResult = true;
        Close();
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        UserSelectedYes = false;
        DialogResult = false;
        Close();
    }
}
