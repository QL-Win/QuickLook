using QuickLook.Common.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace QuickLook.Plugin.CertViewer;

public sealed class Plugin : IViewer
{
    private static readonly HashSet<string> WellKnownExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".p7s", ".pkcs7", // PKCS #7 detached signature (signature only, no original content)
        ".p12", // PKCS #12 certificate store (usually contains certificate and private key)
        ".pfx", // PKCS #12 certificate store (similar to .p12, common on Windows / IIS / .NET)
        ".cer", // Certificate file (DER or PEM encoded, usually contains only the public certificate)
        ".crt", // Certificate file (similar to .cer, common on UNIX/Linux)
        ".pem", // PEM encoded certificate or key file (can contain certificate, private key, or CA chain)
        //".snk", // Strong Name Key file (.NET strong name key pair)
        //".pvk", // Private key file (usually used with .spc)
        //".spc", // Software Publisher Certificate
        ".mobileprovision", // Apple mobile device provisioning profile (contains certificates, public keys, etc.)
        ".certSigningRequest", // Certificate Signing Request (usually .csr)
        //".csr", // Certificate Signing Request
        //".keystore", // Java keystore file (usually stores certificates and private keys)
    };

    private CertViewerControl _control;
    private string _currentPath;

    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        if (Directory.Exists(path))
            return false;

        var ext = Path.GetExtension(path);
        if (!string.IsNullOrEmpty(ext) && WellKnownExtensions.Contains(ext))
            return true;

        return false;
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 800, Height = 600 };
    }

    public void View(string path, ContextObject context)
    {
        _currentPath = path;

        context.IsBusy = true;

        _control = new CertViewerControl();
        _control.LoadFromPath(path);

        context.ViewerContent = _control;
        context.Title = Path.GetFileName(path);
        context.IsBusy = false;
    }

    public void Cleanup()
    {
        _control?.Dispose();
        _control = null;
    }
}
