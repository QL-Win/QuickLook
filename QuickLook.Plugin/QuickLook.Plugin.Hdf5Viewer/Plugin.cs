using QuickLook.Common.Plugin;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace QuickLook.Plugin.Hdf5Viewer;

public sealed class Plugin : IViewer
{
    private static readonly byte[] Hdf5Signature = { 0x89, 0x48, 0x44, 0x46, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly string[] SupportedExtensions = { ".h5", ".hdf5", ".hdf", ".he5" };
    private static readonly long[] SignatureProbeOffsets = { 0, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288 };
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
        context.IsBusy = true;

        var panel = _panel;

        Task.Run(() =>
        {
            try
            {
                return Hdf5SummaryBuilder.Build(path);
            }
            catch (Exception ex)
            {
                return
                    $"Failed to open HDF5 file.{Environment.NewLine}{Environment.NewLine}" +
                    $"{ex.GetType().Name}: {ex.Message}";
            }
        }).ContinueWith(t =>
        {
            if (panel is not null)
                panel.SetText(t.Result);

            context.IsBusy = false;
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void Cleanup()
    {
        _panel = null;
    }

    private static bool HasHdf5Signature(string path)
    {
        try
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (stream.Length < Hdf5Signature.Length)
                    return false;

                var header = new byte[Hdf5Signature.Length];

                foreach (var offset in SignatureProbeOffsets)
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
