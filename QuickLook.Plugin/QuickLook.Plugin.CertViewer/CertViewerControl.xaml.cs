using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Controls;

namespace QuickLook.Plugin.CertViewer;

public partial class CertViewerControl : UserControl, IDisposable
{
    private string _currentPath;

    public CertViewerControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Load a certificate file from path. If the file appears password-protected,
    /// the control will show an inline password input and allow the user to retry.
    /// </summary>
    public void LoadFromPath(string path)
    {
        _currentPath = path;

        var result = CertUtils.TryLoadCertificate(path);

        if (!result.Success && result.NeedsPassword)
        {
            // show password UI
            PasswordPanel.Visibility = System.Windows.Visibility.Visible;
            PropertyList.ItemsSource = null;
            RawText.Text = string.Empty;
            return;
        }

        PasswordPanel.Visibility = System.Windows.Visibility.Collapsed;

        if (result.Success && result.Certificate != null)
            LoadCertificate(result.Certificate);
        else
            LoadRaw(path, result.Message, result.RawContent);
    }

    public void LoadCertificate(X509Certificate2 cert)
    {
        PasswordPanel.Visibility = System.Windows.Visibility.Collapsed;
        var items = new List<KeyValuePair<string, string>>
        {
            new("Subject", cert.Subject),
            new("Issuer", cert.Issuer),
            new("Thumbprint", cert.Thumbprint),
            new("SerialNumber", cert.SerialNumber),
            new("NotBefore", cert.NotBefore.ToString()),
            new("NotAfter", cert.NotAfter.ToString()),
            new("SignatureAlgorithm", cert.SignatureAlgorithm.FriendlyName ?? cert.SignatureAlgorithm.Value),
            new("PublicKey", cert.PublicKey.Oid.FriendlyName ?? cert.PublicKey.Oid.Value),
        };

        PropertyList.ItemsSource = items;
        RawText.Text = string.Empty;
    }

    public void LoadRaw(string path, string message, string content)
    {
        PasswordPanel.Visibility = System.Windows.Visibility.Collapsed;
        PropertyList.ItemsSource = new List<KeyValuePair<string, string>>
        {
            new("Path", path),
            new("Info", message)
        };

        RawText.Text = content ?? "(No content to display)";
    }

    public void Dispose()
    {
    }

    private void LoadWithPasswordButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var pwd = InlinePasswordBox.Password;
        if (string.IsNullOrEmpty(_currentPath))
            return;

        var result = CertUtils.TryLoadCertificate(_currentPath, pwd);
        PasswordPanel.Visibility = System.Windows.Visibility.Collapsed;

        if (result.Success && result.Certificate != null)
        {
            LoadCertificate(result.Certificate);
        }
        else
        {
            LoadRaw(_currentPath, result.Message, result.RawContent);
        }
    }

    private void CancelPasswordButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        // show failure/raw view
        if (string.IsNullOrEmpty(_currentPath))
            return;

        var result = CertUtils.TryLoadCertificate(_currentPath);
        PasswordPanel.Visibility = System.Windows.Visibility.Collapsed;
        LoadRaw(_currentPath, result.Message, result.RawContent);
    }
}
