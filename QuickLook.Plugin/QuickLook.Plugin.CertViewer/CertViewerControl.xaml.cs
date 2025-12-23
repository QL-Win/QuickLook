using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Controls;

namespace QuickLook.Plugin.CertViewer;

public partial class CertViewerControl : UserControl, IDisposable
{
    public CertViewerControl()
    {
        InitializeComponent();
    }

    public void LoadCertificate(X509Certificate2 cert)
    {
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
}
