using QuickLook.Common.Plugin;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace QuickLook.Plugin.Hdf5Viewer;

public sealed class Plugin : IViewer
{
    private static readonly byte[] Hdf5Signature = { 0x89, 0x48, 0x44, 0x46, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly string[] SupportedExtensions = { ".h5", ".hdf5", ".hdf", ".he5" };
    private Hdf5TextPanel _panel;

    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        if (Directory.Exists(path))
            return false;

        var extension = Path.GetExtension(path);
        if (!SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return false;

        return HasHdf5Signature(path);
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 1100, Height = 760 };
    }

    public void View(string path, ContextObject context)
    {
        _panel = new Hdf5TextPanel();
        context.ViewerContent = _panel;
        context.Title = Path.GetFileName(path);

        try
        {
            _panel.SetText(Hdf5SummaryBuilder.Build(path));
        }
        catch (Exception ex)
        {
            _panel.SetText(
                $"Failed to open HDF5 file.{Environment.NewLine}{Environment.NewLine}" +
                $"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            context.IsBusy = false;
        }
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);
        _panel = null;
    }

    private static bool HasHdf5Signature(string path)
    {
        var probes = new long[12];
        probes[0] = 0;

        for (var i = 1; i < probes.Length; i++)
            probes[i] = 512L << (i - 1);

        try
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (stream.Length < Hdf5Signature.Length)
                    return false;

                var header = new byte[Hdf5Signature.Length];

                foreach (var offset in probes)
                {
                    if (offset + Hdf5Signature.Length > stream.Length)
                        break;

                    stream.Position = offset;

                    var read = stream.Read(header, 0, header.Length);
                    if (read != header.Length)
                        continue;

                    if (header.SequenceEqual(Hdf5Signature))
                        return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
