using System.Windows;

namespace QuickLook.Plugin.OfficeViewer;

public partial class ProtectedViewDialog : Window
{
    public bool RememberChoice => RememberChoiceCheckBox.IsChecked == true;

    public bool Choice { get; private set; }

    public ProtectedViewDialog()
    {
        InitializeComponent();
    }

    private void ButtonYes_Click(object sender, RoutedEventArgs e)
    {
        Choice = true;
        DialogResult = true;
    }

    private void ButtonNo_Click(object sender, RoutedEventArgs e)
    {
        Choice = false;
        DialogResult = true;
    }
}
