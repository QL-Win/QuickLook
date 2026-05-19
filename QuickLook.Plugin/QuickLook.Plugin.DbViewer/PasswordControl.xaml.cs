using System;
using System.Windows;
using System.Windows.Controls;

namespace QuickLook.Plugin.DbViewer;

public partial class PasswordControl : UserControl
{
    public event Func<string, bool> PasswordRequested;

    public string Password => passwordBox.Dispatcher.Invoke(() => passwordBox.Password);

    public PasswordControl()
    {
        InitializeComponent();

        openButton.Click += OpenButton_Click;
        cancelButton.Click += CancelButton_Click;
        passwordBox.KeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                OpenButton_Click(s, null);
        };
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var pw = Password;
        if (string.IsNullOrEmpty(pw))
            return;

        passwordErrorTextBlock.Visibility = Visibility.Collapsed;

        bool accepted = PasswordRequested?.Invoke(pw) ?? false;
        if (!accepted)
            passwordErrorTextBlock.Dispatcher.Invoke(() => passwordErrorTextBlock.Visibility = Visibility.Visible);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)?.Close();
    }
}
