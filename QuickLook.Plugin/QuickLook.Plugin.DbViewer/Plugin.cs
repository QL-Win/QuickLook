using Microsoft.Data.Sqlite;
using QuickLook.Common.Commands;
using QuickLook.Common.Controls;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Common.Plugin.MoreMenu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;

namespace QuickLook.Plugin.DbViewer;

public sealed partial class Plugin : IViewer, IMoreMenu
{
    private DbViewerPanel _panel;
    private PasswordControl _passwordControl;
    private string _currentPath;

    public int Priority => 0;

    public IEnumerable<IMenuItem> MenuItems => GetMenuItems();

    public void Init()
    {
        try
        {
            SQLitePCL.Batteries_V2.Init();
        }
        catch
        {
            // Provider may already be initialised by another component; proceed.
        }
    }

    public bool CanHandle(string path)
    {
        if (Directory.Exists(path))
            return false;

        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not ".sqlite" and not ".sqlite3" and not ".db" and not ".db3" and not ".sdb" and not ".litedb" and not ".lite")
            return false;

        try
        {
            return DetectDatabaseType(path) != DatabaseType.Unknown;
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            // File is locked/inaccessible — report as handleable so View() can show the error.
            return true;
        }
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

        DatabaseType dbType;
        try
        {
            dbType = DetectDatabaseType(path);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            var errorBlock = new System.Windows.Controls.TextBlock
            {
                Text = ex.Message,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new System.Windows.Thickness(24),
                Foreground = System.Windows.Media.Brushes.OrangeRed,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            };
            context.ViewerContent = errorBlock;
            context.Title = Path.GetFileName(path);
            context.IsBusy = false;
            return;
        }

        if (dbType == DatabaseType.EncryptedSQLite)
        {
            _passwordControl = new PasswordControl();
            _passwordControl.PasswordRequested += password =>
            {
                // Verify the password
                try
                {
                    var csb = new SqliteConnectionStringBuilder { DataSource = path, Password = password };
                    using var conn = new SqliteConnection(csb.ToString());
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT 1";
                    cmd.ExecuteScalar();
                }
                catch
                {
                    return false;
                }

                // Password accepted — switch to the data panel
                _panel = new DbViewerPanel();
                context.ViewerContent = _panel;
                context.Title = Path.GetFileName(path);
                _panel.LoadDatabase(path, password);
                context.IsBusy = false;
                return true;
            };

            context.ViewerContent = _passwordControl;
            context.Title = $"[ENCRYPTED] {Path.GetFileName(path)}";
            context.IsBusy = false;
            return;
        }

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
        _passwordControl = null;
        // SQLite uses a connection pool; disposing a connection returns it to the pool
        // but does not close the underlying file handle. Clear the pool to release the lock.
        SqliteConnection.ClearAllPools();
    }

    internal static DatabaseType DetectDatabaseType(string path)
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
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            // File is locked or access denied — rethrow so callers can show a meaningful error.
            throw;
        }
        catch
        {
            // Ignore other invalid file reads during detection.
        }

        // Header-based detection failed.
        // Encrypted SQLite files have no recognisable header; use the extension
        // as a heuristic so we can show the password dialog.
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is ".sqlite" or ".sqlite3" or ".db" or ".db3" or ".sdb")
            return DatabaseType.EncryptedSQLite;

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
                Header = TranslationHelper.Get("MW_ExportToExcel", domain: Assembly.GetExecutingAssembly().GetName().Name),
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
}

internal enum DatabaseType
{
    Unknown,
    SQLite,
    EncryptedSQLite,
    LiteDb,
}
