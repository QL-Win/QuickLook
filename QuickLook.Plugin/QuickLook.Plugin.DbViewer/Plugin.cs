using QuickLook.Common.Commands;
using QuickLook.Common.Controls;
using QuickLook.Common.Plugin;
using QuickLook.Common.Plugin.MoreMenu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace QuickLook.Plugin.DbViewer;

public sealed partial class Plugin : IViewer, IMoreMenu
{
    private DbViewerPanel _panel;
    private string _currentPath;

    public int Priority => 0;

    public IEnumerable<IMenuItem> MenuItems => GetMenuItems();

    public void Init()
    {
    }

    public bool CanHandle(string path)
    {
        if (Directory.Exists(path))
            return false;

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not ".sqlite" and not ".sqlite3" and not ".db" and not ".db3" and not ".sdb" and not ".litedb" and not ".lite")
            return false;

        return DetectDatabaseType(path) != DatabaseType.Unknown;
    }

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size { Width = 1024, Height = 720 };
        context.TitlebarOverlap = false;
        context.TitlebarBlurVisibility = true;
        context.TitlebarColourVisibility = true;
    }

    public void View(string path, ContextObject context)
    {
        _currentPath = path;
        _panel = new DbViewerPanel();

        context.ViewerContent = _panel;
        context.Title = Path.GetFileName(path);
        _panel.LoadDatabase(path);

        context.IsBusy = false;
    }

    public void Cleanup()
    {
        GC.SuppressFinalize(this);
        _panel = null;
    }

    private static DatabaseType DetectDatabaseType(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var header = new byte[59]; // need 32+27 bytes for LiteDB magic
            var count = stream.Read(header, 0, header.Length);

            if (count >= 16)
            {
                var text = System.Text.Encoding.ASCII.GetString(header, 0, 16);
                if (text.StartsWith("SQLite format 3", StringComparison.Ordinal))
                    return DatabaseType.SQLite;
            }

            // LiteDB 5.x: magic string "** This is a LiteDB file **" at offset 32
            if (count >= 59)
            {
                var liteDbMagic = System.Text.Encoding.ASCII.GetString(header, 32, 27);
                if (liteDbMagic == "** This is a LiteDB file **")
                    return DatabaseType.LiteDb;
            }
        }
        catch
        {
            // Ignore invalid file reads during detection.
        }

        return DatabaseType.Unknown;
    }

    private IEnumerable<IMenuItem> GetMenuItems()
    {
        if (_panel is null || !_panel.HasLoadedData)
            return [];

        return
        [
            new MoreMenuItem
            {
                Icon = FontSymbols.SaveAs,
                Header = "导出为 Excel",
                Command = new RelayCommand(ExportToExcel),
                IsVisible = true,
                IsEnabled = true,
                MenuItems = null,
            }
        ];
    }

    private void ExportToExcel()
    {
        if (_panel is null)
            return;

        _panel.ExportCurrentTableToExcel(_currentPath);
    }

    private enum DatabaseType
    {
        Unknown,
        SQLite,
        LiteDb,
    }
}
