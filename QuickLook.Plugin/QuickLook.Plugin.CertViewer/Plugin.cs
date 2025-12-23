using QuickLook.Common.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace QuickLook.Plugin.CertViewer;

public class Plugin : IViewer
{
    private static readonly HashSet<string> WellKnownExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".p12",
        ".pfx",
        ".cer",
        ".crt",
        ".pem",
        ".snk",
        ".pvk",
        ".spc",
        ".mobileprovision",
        ".certSigningRequest",
        ".csr",
        ".keystore",
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
