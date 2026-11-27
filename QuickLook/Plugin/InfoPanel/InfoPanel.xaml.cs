// Copyright © 2017-2026 QL-Win Contributors
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using QuickLook.Helpers;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QuickLook.Plugin.InfoPanel;

public partial class InfoPanel : UserControl
{
    private const string UnknownValue = "—";

    private bool _stop;
    private bool _statusOverride;

    public InfoPanel()
    {
        InitializeComponent();
    }

    public bool Stop
    {
        set => _stop = value;
        get => _stop;
    }

    /// <summary>
    ///     Shows a custom status line instead of the size. Used while a shell item is copied to a
    ///     temporary file; the normal metadata updates are suppressed until this panel is replaced.
    /// </summary>
    public void SetStatusText(string text)
    {
        _statusOverride = true;

        RunOnUiThread(() => { totalSize.Text = text; });
    }

    /// <summary>
    ///     Shows a display name without running the full metadata resolution. Used by the placeholder
    ///     preview while a shell item is being copied.
    /// </summary>
    public void SetDisplayName(string name)
    {
        var text = string.IsNullOrEmpty(name) ? UnknownValue : name;

        RunOnUiThread(() => { filename.Text = text; });
    }

    public void DisplayInfo(string path)
    {
        // DisplayInfo is called for *any* path Explorer hands over, including shell parsing names of
        // MTP device items that System.IO rejects with "Illegal characters in path". This method must
        // never throw, otherwise the whole preview fails; fall back to the shell property system instead.
        try
        {
            DisplayThumbnail(path);

            filename.Text = GetSafeDisplayName(path);

            var lastModified = GetSafeLastModified(path);
            modDate.Text = string.Format(TranslationHelper.Get("InfoPanel_LastModified"),
                lastModified?.ToString(CultureInfo.CurrentCulture) ?? UnknownValue);

            Stop = false;

            _ = Task.Run(() =>
            {
                try
                {
                    UpdateSizeText(path);
                }
                catch (Exception e)
                {
                    WriteLog(e);
                }
            });
        }
        catch (Exception e)
        {
            WriteLog(e);
        }
    }

    private void DisplayThumbnail(string path)
    {
        _ = Task.Run(() =>
        {
            try
            {
                var scale = DisplayDeviceHelper.GetCurrentScaleFactor();

                var icon =
                    WindowsThumbnailProvider.GetThumbnail(path,
                        (int)(128 * scale.Horizontal),
                        (int)(128 * scale.Vertical),
                        ThumbnailOptions.ScaleUp);

                var source = icon?.ToBitmapSource();
                icon?.Dispose();

                Dispatcher.BeginInvoke(new Action(() => image.Source = source));
            }
            catch (Exception e)
            {
                WriteLog(e);
            }
        });
    }

    private void UpdateSizeText(string path)
    {
        if (string.IsNullOrEmpty(path) || _statusOverride)
            return;

        long? fileSize = null;

        try
        {
            if (File.Exists(path))
            {
                try
                {
                    fileSize = new FileInfo(path).Length;
                }
                catch (Exception)
                {
                    // ignored, fall back to the shell property system below
                }
            }
        }
        catch (Exception)
        {
            // System.IO rejects shell parsing names; keep fileSize null
        }

        if (fileSize.HasValue)
        {
            var size = fileSize.Value;

            RunOnUiThread(() => { totalSize.Text = size.ToPrettySize(2); });
            return;
        }

        var isDriveRoot = false;

        try
        {
            isDriveRoot = string.Equals(Path.GetPathRoot(path), path, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            // not a Win32 path
        }

        if (isDriveRoot) // is this a drive?
        {
            FileHelper.GetDriveSpace(path, out var totalSpace, out var totalFreeSpace);

            RunOnUiThread(() =>
            {
                totalSize.Text =
                    string.Format(TranslationHelper.Get("InfoPanel_DriveSize"),
                        totalSpace.ToPrettySize(2),
                        totalFreeSpace.ToPrettySize(2));
            });

            return;
        }

        if (Directory.Exists(path))
        {
            FileHelper.CountFolder(path, ref _stop,
                out var totalDirsL, out var totalFilesL, out var totalSizeL);

            if (!Stop)
                RunOnUiThread(() =>
                {
                    string t;
                    var folders = totalDirsL == 0
                        ? string.Empty
                        : string.Format(TranslationHelper.Get(
                            totalDirsL == 1 ? "InfoPanel_Folder" : "InfoPanel_Folders"), totalDirsL);
                    var files = totalFilesL == 0
                        ? string.Empty
                        : string.Format(TranslationHelper.Get(
                            totalFilesL == 1 ? "InfoPanel_File" : "InfoPanel_Files"), totalFilesL);

                    if (!string.IsNullOrEmpty(folders) && !string.IsNullOrEmpty(files))
                        t = string.Format(
                            TranslationHelper.Get("InfoPanel_FolderAndFile"), folders, files);
                    else if (string.IsNullOrEmpty(folders) && string.IsNullOrEmpty(files))
                        t = string.Empty;
                    else
                        t = $"({folders}{files})";

                    totalSize.Text =
                        $"{totalSizeL.ToPrettySize(2)} {t}";
                });

            return;
        }

        // Shell-only item (file on an MTP device, item inside a zip folder, ...). The basic metadata is
        // read from the shell namespace, so nothing has to be copied to the local disk.
        if (ShellItemHelper.TryGetShellItemMetadata(path,
                out _, out var shellOnlyIsFolder, out var shellOnlySize, out _, out var shellOnlyType))
        {
            if (shellOnlyIsFolder)
            {
                // A folder is never copied; its size is computed from the shell metadata in the background
                // (this method already runs on a worker thread), so the name/date show up immediately.
                RunOnUiThread(() => { totalSize.Text = TranslationHelper.Get("InfoPanel_CalculatingSize", failsafe: "Calculating size…"); });

                if (ShellItemHelper.TryGetShellFolderSize(path, () => Stop,
                        out var folderSize, out var folderFiles, out var folderDirs,
                        (sizeSoFar, filesSoFar, dirsSoFar) =>
                        {
                            if (!Stop)
                            {
                                RunOnUiThread(() => { totalSize.Text =
                                    $"{TranslationHelper.Get("InfoPanel_CalculatingSize", failsafe: "Calculating size…")} " +
                                    $"{sizeSoFar.ToPrettySize(1)} ({filesSoFar} files so far)"; });
                            }
                        }))
                {
                    if (!Stop)
                        RunOnUiThread(() => { totalSize.Text = FormatFolderSizeText(folderSize, folderDirs, folderFiles); });
                }
                else if (!Stop)
                {
                    // The enumeration failed (device unplugged, access denied, safety limit, ...): do not
                    // leave "Calculating size…" behind and do not present a partial total as final size.
                    var unavailable = string.IsNullOrEmpty(shellOnlyType)
                        ? TranslationHelper.Get("InfoPanel_SizeUnavailable", failsafe: "Size unavailable")
                        : shellOnlyType;

                    RunOnUiThread(() => { totalSize.Text = unavailable; });
                }

                return;
            }

            if (shellOnlySize > 0)
            {
                RunOnUiThread(() => { totalSize.Text = shellOnlySize.ToPrettySize(2); });
                return;
            }

            if (!string.IsNullOrEmpty(shellOnlyType))
                RunOnUiThread(() => { totalSize.Text = shellOnlyType; });

            return;
        }

        // A shell folder whose namespace walk failed (for example a folder inside a zip archive): at
        // least report its type instead of leaving the default "Calculating size..." forever.
        try
        {
            if (ShellItemInfo.TryGetIsFolder(path, out var parsedIsFolder) && parsedIsFolder)
            {
                var parsedFolderType = ShellItemInfo.TryGetTypeText(path, out var parsedType) && !string.IsNullOrEmpty(parsedType)
                    ? parsedType
                    : "File folder";

                RunOnUiThread(() => { totalSize.Text = parsedFolderType; });
                return;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        if (ShellItemInfo.TryGetSize(path, out var shellSize))
        {
            RunOnUiThread(() => { totalSize.Text = shellSize.ToPrettySize(2); });
            return;
        }

        if (ShellItemInfo.TryGetTypeText(path, out var typeText) && !string.IsNullOrEmpty(typeText))
            RunOnUiThread(() => { totalSize.Text = typeText; });
    }

    /// <summary>
    ///     Formats a folder size the same way the local folder branch of <see cref="UpdateSizeText" /> does.
    /// </summary>
    private static string FormatFolderSizeText(long totalSizeL, int totalDirsL, int totalFilesL)
    {
        var folders = totalDirsL == 0
            ? string.Empty
            : string.Format(TranslationHelper.Get(totalDirsL == 1 ? "InfoPanel_Folder" : "InfoPanel_Folders"), totalDirsL);
        var files = totalFilesL == 0
            ? string.Empty
            : string.Format(TranslationHelper.Get(totalFilesL == 1 ? "InfoPanel_File" : "InfoPanel_Files"), totalFilesL);

        string t;

        if (!string.IsNullOrEmpty(folders) && !string.IsNullOrEmpty(files))
            t = string.Format(TranslationHelper.Get("InfoPanel_FolderAndFile"), folders, files);
        else if (string.IsNullOrEmpty(folders) && string.IsNullOrEmpty(files))
            t = string.Empty;
        else
            t = $"({folders}{files})";

        return $"{totalSizeL.ToPrettySize(2)} {t}";
    }

    internal static string GetSafeDisplayName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        var isShellOnly = false;

        try
        {
            isShellOnly = !File.Exists(path) && !Directory.Exists(path);
        }
        catch (Exception)
        {
            isShellOnly = true;
        }

        if (isShellOnly)
        {
            // Shell parsing names cannot be passed to System.IO and their last segment is an internal
            // item id, not a name. Ask the shell namespace for the real display name first.
            if (ShellItemHelper.TryGetShellItemMetadata(path, out var shellOnlyName, out _, out _, out _, out _)
             && !string.IsNullOrEmpty(shellOnlyName))
            {
                return shellOnlyName;
            }

            if (ShellItemInfo.TryGetDisplayName(path, out var parsedShellName) && !string.IsNullOrEmpty(parsedShellName))
                return parsedShellName;

            return path;
        }

        if (ShellItemInfo.TryGetDisplayName(path, out var shellName) && !string.IsNullOrEmpty(shellName))
            return shellName;

        try
        {
            var name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(name))
                return name;
        }
        catch (Exception)
        {
            // Illegal characters in path: give up and show the raw path.
        }

        return path;
    }

    private static DateTime? GetSafeLastModified(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        try
        {
            var lastWriteTime = File.GetLastWriteTime(path);

            // 1601-01-01 means "file not found" for System.IO.
            if (lastWriteTime.Year > 1601)
                return lastWriteTime;
        }
        catch (Exception)
        {
            // Illegal characters in path: use the shell property below.
        }

        if (ShellItemInfo.TryGetDateModified(path, out var shellModified))
            return shellModified;

        // MTP/portable device items: SHCreateItemFromParsingName cannot read them, walk the shell namespace.
        if (ShellItemHelper.TryGetShellItemMetadata(path, out _, out _, out _, out var shellOnlyModified, out _)
         && shellOnlyModified.HasValue)
        {
            return shellOnlyModified;
        }

        return null;
    }

    private void RunOnUiThread(Action action)
    {
        try
        {
            Dispatcher.Invoke(action);
        }
        catch (Exception)
        {
            // The preview window is gone.
        }
    }

    private static void WriteLog(Exception e)
    {
        try
        {
            ProcessHelper.WriteLog(e.ToString());
        }
        catch (Exception)
        {
            // ignored
        }
    }
}
