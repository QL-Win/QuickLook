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

using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace QuickLook;

public class ViewWindowManager : IDisposable
{
    private static ViewWindowManager _instance;

    private string _invokedPath = string.Empty;
    private string _previewPath = string.Empty;
    private string _materializedTempFile;
    private ViewerWindow _viewerWindow;

    internal ViewWindowManager()
    {
        InitNewViewerWindow();
    }

    public void Dispose()
    {
        StopFocusMonitor();
    }

    public void RunAndClosePreview()
    {
        if (!_viewerWindow.IsVisible)
            return;

        // if the current focus is in Desktop or explorer windows, just close the preview window and leave the task to System.
        var focus = NativeMethods.QuickLook.GetFocusedWindowType();
        if (focus != NativeMethods.QuickLook.FocusedWindowType.Invalid)
        {
            StopFocusMonitor();
            _viewerWindow.Close();
            return;
        }

        // if the focus is in the preview window, run it
        if (!WindowHelper.IsForegroundWindowBelongToSelf())
            return;

        StopFocusMonitor();
        _viewerWindow.RunAndClose();
    }

    public void ClosePreview()
    {
        if (!_viewerWindow.IsVisible)
            return;

        StopFocusMonitor();
        _viewerWindow.Close();
    }

    public void TogglePreview(string path = null, string options = null)
    {
        if (string.IsNullOrEmpty(path))
            path = NativeMethods.QuickLook.GetCurrentSelection();

        if (!string.IsNullOrEmpty(options))
            InvokePreviewWithOption(path, options);
        else
            if (_viewerWindow.IsVisible && (string.IsNullOrEmpty(path) || path == _invokedPath))
                ClosePreview();
            else
                InvokePreview(path);
    }

    private void RunFocusMonitor()
    {
        FocusMonitor.GetInstance().Start();
    }

    private void StopFocusMonitor()
    {
        FocusMonitor.GetInstance().Stop();
    }

    internal void ForgetCurrentWindow()
    {
        StopFocusMonitor();

        _viewerWindow.Pinned = true;

        InitNewViewerWindow();
    }

    public void SwitchPreview(string path = null)
    {
        if (!_viewerWindow.IsVisible)
            return;

        if (string.IsNullOrEmpty(path))
            path = NativeMethods.QuickLook.GetCurrentSelection();

        if (string.IsNullOrEmpty(path))
            return;

        InvokePreview(path);
    }

    public void InvokePreviewWithOption(string path = null, string options = null)
    {
        InvokePreview(path);

        if (string.IsNullOrWhiteSpace(options)) return;

        var cli = new CommandLineParser(options.Split(','));

        if (cli.Has("top"))
        {
            _viewerWindow.Topmost = true;
            _viewerWindow.buttonTop.Tag = "Top";
        }
        if (cli.Has("pin"))
        {
            _viewerWindow.Pinned = true;
            ForgetCurrentWindow();
        }
    }

    public void InvokePreview(string path = null)
    {
        if (string.IsNullOrEmpty(path))
            path = NativeMethods.QuickLook.GetCurrentSelection();

        if (string.IsNullOrEmpty(path))
            return;

        if (_viewerWindow.IsVisible && path == _invokedPath)
            return;

        var previewPath = path;
        var useDefaultPlugin = false;
        IViewer matchedPlugin = null;

        var isDirectory = Directory.Exists(path);

        if (!isDirectory && !File.Exists(path))
        {
            if (!path.StartsWith("::", StringComparison.Ordinal)) // CLSID
                return;

            // Shell parsing names of MTP items do not contain the file name (the last segment is an item
            // id). A folder is shown by the info panel: its size is computed from shell metadata and
            // nothing is copied. Files are copied to a temporary file so that the regular plugins (image,
            // video, archive, text, ...) can preview them exactly like before.
            var hasShellMetadata = ShellItemHelper.TryGetShellItemMetadata(path,
                out _, out var shellItemIsFolder, out _, out _, out _);

            if (hasShellMetadata && shellItemIsFolder)
            {
                previewPath = path;
                useDefaultPlugin = true;
            }
            else
            {
                var materializedPath = ShellItemHelper.TryMaterialize(path);

                if (!string.IsNullOrEmpty(materializedPath))
                {
                    // Extension filter settings must also protect previews coming from a device.
                    if (!ExtensionFilterHelper.IsExtensionAllowed(materializedPath))
                    {
                        ShellItemHelper.TryDeleteTempFile(materializedPath);
                        return;
                    }

                    // Some plugins (video, media info, ...) inspect the content instead of the extension,
                    // so the plugin match can only be decided on a real local file. When no plugin can
                    // preview it, the info panel shows the original shell item (real name/date/size);
                    // the temporary copy is then dropped so nothing unnecessary is kept on disk.
                    var matchedForCopy = PluginManager.GetInstance().FindMatch(materializedPath);

                    if (matchedForCopy is Plugin.InfoPanel.Plugin)
                    {
                        ShellItemHelper.TryDeleteTempFile(materializedPath);
                        previewPath = path;
                        useDefaultPlugin = true;
                    }
                    else
                    {
                        matchedPlugin = matchedForCopy;
                        previewPath = materializedPath;

                        // Delete the temporary copy when the preview window closes (or after a grace
                        // period when another preview replaces this one, see below).
                        _materializedTempFile = previewPath;
                        _viewerWindow.Closed += (sender, e) =>
                        {
                            var tempFileToDelete = _materializedTempFile;
                            _materializedTempFile = null;

                            if (!string.IsNullOrEmpty(tempFileToDelete))
                            {
                                _ = Task.Delay(TimeSpan.FromSeconds(5))
                                    .ContinueWith(_ => ShellItemHelper.TryDeleteTempFile(tempFileToDelete), TaskScheduler.Default);
                            }
                        };
                    }
                }
                else
                {
                    // Exact "::{GUID}" namespace roots (Recycle Bin, This PC, ...) are handled by CLSIDViewer.
                    // Any other shell-only item that could not be copied (too large, access denied, ...) must
                    // never be handed to a file based plugin: it cannot open such a path.
                    previewPath = path;
                    useDefaultPlugin = !ShellItemHelper.IsExactClsidPath(path);
                }
            }
        }
        else if (!isDirectory && !ExtensionFilterHelper.IsExtensionAllowed(path))
        {
            // Check extension filtering before proceeding (skip for directories)
            return;
        }

        // A new preview replaces the previous one: drop the temporary copy of the replaced shell item.
        // The previous plugin may still be finishing an asynchronous load of that file (image decoding,
        // archive listing, ...), therefore remove it after a grace period instead of immediately.
        var previousTempFile = _materializedTempFile;

        if (!string.IsNullOrEmpty(previousTempFile))
        {
            _materializedTempFile = null;
            _ = Task.Delay(TimeSpan.FromSeconds(20))
                .ContinueWith(_ => ShellItemHelper.TryDeleteTempFile(previousTempFile), TaskScheduler.Default);
        }

        _invokedPath = path;
        _previewPath = previewPath;

        RunFocusMonitor();

        matchedPlugin ??= useDefaultPlugin
            ? PluginManager.GetInstance().DefaultPlugin
            : PluginManager.GetInstance().FindMatch(previewPath);

        BeginShowNewWindow(previewPath, matchedPlugin);
    }

    public void InvokePluginPreview(string plugin, string path = null)
    {
        if (string.IsNullOrEmpty(path))
            path = _invokedPath;

        if (string.IsNullOrEmpty(path))
            return;

        var isDirectory = Directory.Exists(path);
        if (!isDirectory && !File.Exists(path))
            return;

        // Check extension filtering before proceeding (skip for directories)
        if (!isDirectory && !ExtensionFilterHelper.IsExtensionAllowed(path))
            return;

        RunFocusMonitor();

        var matchedPlugin = PluginManager.GetInstance().LoadedPlugins.Find(p =>
        {
            return p.GetType().Assembly.GetName().Name == plugin;
        });

        if (matchedPlugin != null)
        {
            BeginShowNewWindow(path, matchedPlugin);
        }
    }

    public void ReloadPreview()
    {
        if (!_viewerWindow.IsVisible || string.IsNullOrEmpty(_invokedPath))
            return;

        // For shell-only items the preview is shown from a materialized temporary file; prefer it while
        // it still exists and fall back to the original path otherwise.
        var previewPath = _previewPath;

        if (string.IsNullOrEmpty(previewPath) || (!File.Exists(previewPath) && !Directory.Exists(previewPath)))
            previewPath = _invokedPath;

        var matchedPlugin = PluginManager.GetInstance().FindMatch(previewPath);

        BeginShowNewWindow(previewPath, matchedPlugin);
    }

    public void ToggleFullscreen()
    {
        if (!_viewerWindow.IsVisible)
            return;

        _viewerWindow.ToggleFullscreen();
    }

    private void BeginShowNewWindow(string path, IViewer matchedPlugin)
    {
        _viewerWindow.UnloadPlugin();

        _viewerWindow.BeginShow(matchedPlugin, path, CurrentPluginFailed);
    }

    private void CurrentPluginFailed(string path, ExceptionDispatchInfo e)
    {
        var plugin = _viewerWindow.Plugin?.GetType();

        _viewerWindow.Close();

        TrayIconManager.ShowNotification($"Failed to preview {Path.GetFileName(path)}",
            "Consider reporting this incident to QuickLook’s author.", true);

        Debug.WriteLine(e.SourceException.ToString());

        ProcessHelper.WriteLog(e.SourceException.ToString());

        if (plugin != PluginManager.GetInstance().DefaultPlugin.GetType())
            BeginShowNewWindow(path, PluginManager.GetInstance().DefaultPlugin);
        else
            e.Throw();
    }

    private void InitNewViewerWindow()
    {
        _viewerWindow = new ViewerWindow();
        _viewerWindow.Closed += (sender, e) =>
        {
            if (ProcessHelper.IsShuttingDown())
                return;
            if (sender is not ViewerWindow w)
                return;
            // Only skip if the window was already forgotten by ForgetCurrentWindow,
            // which sets Pinned=true AND replaces _viewerWindow with a new instance.
            if (w.Pinned && _viewerWindow != w)
                return;
            StopFocusMonitor();
            InitNewViewerWindow();
        };
    }

    public static ViewWindowManager GetInstance()
    {
        return _instance ??= new ViewWindowManager();
    }
}
