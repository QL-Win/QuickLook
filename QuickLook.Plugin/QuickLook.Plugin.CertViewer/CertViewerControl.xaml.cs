using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
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
            // show password overlay, hide main content
            PasswordOverlay.Visibility = Visibility.Visible;
            MainTab.Visibility = Visibility.Collapsed;
            InlinePasswordBox.Password = string.Empty;
            return;
        }

        PasswordOverlay.Visibility = Visibility.Collapsed;
        MainTab.Visibility = Visibility.Visible;

        if (result.Success && result.Certificate != null)
            LoadCertificate(result.Certificate);
        else
            LoadRaw(path, result.Message, result.RawContent);
    }

    public void LoadCertificate(X509Certificate2 cert)
    {
        PasswordOverlay.Visibility = Visibility.Collapsed;
        MainTab.Visibility = Visibility.Visible;
        var items = new List<KeyValuePair<string, string>>
        {
            new("[Version]", "V" + cert.Version),
            new("[Subject]", cert.SubjectName.Name)
        };
        string nameInfo = cert.GetNameInfo(X509NameType.SimpleName, false);
        if (!string.IsNullOrEmpty(nameInfo)) items.Add(new("  Simple Name", nameInfo));
        string email = cert.GetNameInfo(X509NameType.EmailName, false);
        if (!string.IsNullOrEmpty(email)) items.Add(new("  Email Name", email));
        string upn = cert.GetNameInfo(X509NameType.UpnName, false);
        if (!string.IsNullOrEmpty(upn)) items.Add(new("  UPN Name", upn));
        string dns = cert.GetNameInfo(X509NameType.DnsName, false);
        if (!string.IsNullOrEmpty(dns)) items.Add(new("  DNS Name", dns));

        // [Issuer]
        items.Add(new("[Issuer]", cert.IssuerName.Name));
        nameInfo = cert.GetNameInfo(X509NameType.SimpleName, true);
        if (!string.IsNullOrEmpty(nameInfo)) items.Add(new("  Simple Name", nameInfo));
        email = cert.GetNameInfo(X509NameType.EmailName, true);
        if (!string.IsNullOrEmpty(email)) items.Add(new("  Email Name", email));
        upn = cert.GetNameInfo(X509NameType.UpnName, true);
        if (!string.IsNullOrEmpty(upn)) items.Add(new("  UPN Name", upn));
        dns = cert.GetNameInfo(X509NameType.DnsName, true);
        if (!string.IsNullOrEmpty(dns)) items.Add(new("  DNS Name", dns));

        // [Serial Number]
        items.Add(new("[Serial Number]", cert.SerialNumber));
        // [Not Before]
        items.Add(new("[Not Before]", cert.NotBefore.ToString()));
        // [Not After]
        items.Add(new("[Not After]", cert.NotAfter.ToString()));
        // [Thumbprint]
        items.Add(new("[Thumbprint]", cert.Thumbprint));
        // [Signature Algorithm]
        items.Add(new("[Signature Algorithm]", cert.SignatureAlgorithm.FriendlyName + " (" + cert.SignatureAlgorithm.Value + ")"));

        // [Public Key]
        var pk = cert.PublicKey;
        items.Add(new("[Public Key]", ""));
        items.Add(new("  Algorithm", pk.Oid.FriendlyName));
        try { items.Add(new("  Length", pk.Key.KeySize.ToString())); } catch { }
        items.Add(new("  Key Blob", pk.EncodedKeyValue.Format(true)));
        items.Add(new("  Parameters", pk.EncodedParameters.Format(true)));

        // [Private Key]
        if (cert.HasPrivateKey)
        {
            items.Add(new("[Private Key]", "Present"));
        }

        // [Extensions]
        if (cert.Extensions != null && cert.Extensions.Count > 0)
        {
            items.Add(new("[Extensions]", ""));
            foreach (var ext in cert.Extensions)
            {
                try
                {
                    string extName = ext.Oid.FriendlyName + " (" + ext.Oid.Value + ")";
                    items.Add(new("  " + extName, ext.Format(true)));
                }
                catch
                {
                }
            }
        }

        PropertyList.ItemsSource = items;
        RawText.Text = cert.ToString();
    }

    public void LoadRaw(string path, string message, string content)
    {
        PasswordOverlay.Visibility = Visibility.Collapsed;
        MainTab.Visibility = Visibility.Visible;
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

    private void LoadWithPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var pwd = InlinePasswordBox.Password;
        if (string.IsNullOrEmpty(_currentPath))
            return;

        var result = CertUtils.TryLoadCertificate(_currentPath, pwd);
        if (!result.Success && result.NeedsPassword)
        {
            // still need password, keep overlay
            InlinePasswordBox.Password = string.Empty;
            return;
        }

        PasswordOverlay.Visibility = Visibility.Collapsed;
        MainTab.Visibility = Visibility.Visible;

        if (result.Success && result.Certificate != null)
        {
            LoadCertificate(result.Certificate);
        }
        else
        {
            LoadRaw(_currentPath, result.Message, result.RawContent);
        }
    }

    private void CancelPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        // show failure/raw view
        if (string.IsNullOrEmpty(_currentPath))
            return;

        var result = CertUtils.TryLoadCertificate(_currentPath);
        PasswordOverlay.Visibility = Visibility.Collapsed;
        MainTab.Visibility = Visibility.Visible;
        LoadRaw(_currentPath, result.Message, result.RawContent);
    }
}
