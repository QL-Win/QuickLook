// Copyright © 2024 ema
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

using QuickLook.Common.Helpers;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace QuickLook.Plugin.PDFViewer;

public partial class PasswordControl : UserControl
{
    public event Func<string, bool> PasswordRequested;

    public bool Result { get; private set; } = false;
    public string Password => passwordBox.Dispatcher.Invoke(() => passwordBox.Password);

    public PasswordControl()
    {
        InitializeComponent();

        string domain = Assembly.GetExecutingAssembly().GetName().Name;
        titleTextBlock.Text = TranslationHelper.Get("PW_Title", domain: domain);
        hintTextBlock.Text = TranslationHelper.Get("Pw_Hint", domain: domain);
        passwordErrorTextBlock.Text = TranslationHelper.Get("PW_Error", domain: domain);
        openFileButton.Content = TranslationHelper.Get("BTN_OpenFile", domain: domain);
        cancelButton.Content = TranslationHelper.Get("BTN_Cancel", domain: domain);
        openFileButton.Click += OpenFileButton_Click;
        cancelButton.Click += CancelButton_Click;
    }

    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(Password))
        {
            Result = true;

            if (PasswordRequested != null)
            {
                bool accepted = PasswordRequested.Invoke(Password);

                if (!accepted)
                {
                    passwordErrorTextBlock.Dispatcher.Invoke(() => passwordErrorTextBlock.Visibility = Visibility.Visible);
                }
            }
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = false;

        if (Window.GetWindow(this) is Window window)
        {
            window.Close();
        }
    }
}
