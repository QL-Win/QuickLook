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
using QuickLook.Common.Plugin;
using QuickLook.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace QuickLook;

public class ViewWindowManager : IDisposable
{
    private static ViewWindowManager _instance;

    private string _invokedPath = string.Empty;
    private string _previewPath = string.Empty;
    private string _materializedTempFile;
    private PreparingPreview _preparingPreview;
    private CancellationTokenSource _materializeCts;
    private long _previewRequestId;
    private ViewerWindow _viewerWindow;

    internal ViewWindowManager()
    {
        InitNewViewerWindow();
    }

    public void Dispose()
    {
        CancelPendingMaterialization();
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

        var isDirectory = Directory.Exists(path);

        if (!isDirectory && !File.Exists(path))
        {
            if (ShellItemHelper.IsExactClsidPath(path))
            {
                // Recycle Bin / This PC / ... are handled by CLSIDViewer.
                ShowPreview(path, path, useDefaultPlugin: false);
                return;
            }

            if (!path.StartsWith("::", StringComparison.Ordinal)
             && !ShellItemHelper.TryGetShellItemMetadata(path, out _, out _, out _, out _, out _))
            {
                // An unresolvable plain path (neither a file, a folder nor a shell namespace name): ignore
                // it instead of opening the info panel for something that does not exist.
                return;
            }

            // Show the placeholder window immediately. The worker resolves the metadata and copies the
            // item in the background, so even a multi-gigabyte device file never blocks the UI thread.
            ShowPreparingPreview(path);
            return;
        }

        if (!isDirectory && !ExtensionFilterHelper.IsExtensionAllowed(path))
            return;

        ShowPreview(path, path, useDefaultPlugin: false);
    }

    /// <summary>
    ///     Shows a preview that needs no shell copy (a local file/folder, an info panel item, ...).
    /// </summary>
    private void ShowPreview(string invokedPath, string previewPath, bool useDefaultPlugin)
    {
        CancelPendingMaterialization();
        ReleasePreviousMaterializedFile();

        _invokedPath = invokedPath;
        _previewPath = previewPath;
        _preparingPreview = null;

        RunFocusMonitor();

        var matchedPlugin = useDefaultPlugin
            ? CreateDefaultPluginInstance()
            : PluginManager.GetInstance().FindMatch(previewPath);

        BeginShowNewWindow(previewPath, matchedPlugin);
    }

    /// <summary>
    ///     Shows the info panel immediately and copies the shell item on a background STA worker. The
    ///     copy result replaces the placeholder only when this request is still the latest one.
    /// </summary>
    private void ShowPreparingPreview(string shellPath)
    {
        CancelPendingMaterialization();
        ReleasePreviousMaterializedFile();

        var preparing = new PreparingPreview();

        _preparingPreview = preparing;
        _invokedPath = shellPath;
        _previewPath = shellPath;

        RunFocusMonitor();
        BeginShowNewWindow(shellPath, preparing);

        var requestId = Interlocked.Increment(ref _previewRequestId);
        StartMaterialization(shellPath, requestId);
    }

    private void StartMaterialization(string shellPath, long requestId)
    {
        var cancellation = new CancellationTokenSource();
        _materializeCts = cancellation;

        var token = cancellation.Token;

        var worker = new Thread(() =>
        {
            string tempFile = null;

            try
            {
                var hasMetadata = ShellItemHelper.TryGetShellItemMetadata(shellPath,
                    out var displayName, out var isFolder, out var knownSize, out _, out _);

                if (hasMetadata && !string.IsNullOrEmpty(displayName))
                    UpdatePreparingInfo(requestId, displayName);

                if (hasMetadata && (isFolder || knownSize > ShellItemHelper.MaxMaterializedSize))
                {
                    // Nothing is copied for a folder or an oversized item: the info panel shows the shell
                    // metadata (name, date, type, computed folder size) instead.
                    PostCompletion(requestId, shellPath, null);
                    return;
                }

                tempFile = ShellItemHelper.TryMaterialize(shellPath, token,
                    (copied, expected) => ReportCopyProgress(requestId, copied, expected));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            // Only detach this worker's source: a newer worker may already own the field.
            Interlocked.CompareExchange(ref _materializeCts, null, cancellation);

            if (token.IsCancellationRequested)
            {
                // Superseded or cancelled: the partial copy is discarded.
                if (!string.IsNullOrEmpty(tempFile))
                    ScheduleTempFileCleanup(tempFile);

                return;
            }

            PostCompletion(requestId, shellPath, tempFile);
        })
        {
            IsBackground = true,
            Name = "QuickLook shell item copy"
        };

        // Shell.Application and IFileOperation expect an STA thread.
        worker.SetApartmentState(ApartmentState.STA);
        worker.Start();
    }

    private void CompleteMaterialization(string invokedPath, string tempFile)
    {
        if (string.IsNullOrEmpty(tempFile) || !File.Exists(tempFile))
        {
            // The copy failed (timeout, device disconnected, ...). Never hand a partial file to a plugin:
            // show the shell metadata instead.
            ShowPreview(invokedPath, invokedPath, useDefaultPlugin: true);
            return;
        }

        if (!ExtensionFilterHelper.IsExtensionAllowed(tempFile))
        {
            ShellItemHelper.TryDeleteTempFile(tempFile);
            ShowPreview(invokedPath, invokedPath, useDefaultPlugin: true);
            return;
        }

        var matchedPlugin = PluginManager.GetInstance().FindMatch(tempFile);

        if (matchedPlugin is Plugin.InfoPanel.Plugin)
        {
            // No plugin can preview this file; the info panel shows the real shell metadata, so the
            // temporary copy is not needed at all.
            ShellItemHelper.TryDeleteTempFile(tempFile);
            ShowPreview(invokedPath, invokedPath, useDefaultPlugin: true);
            return;
        }

        ReleasePreviousMaterializedFile();

        _invokedPath = invokedPath;
        _previewPath = tempFile;
        _materializedTempFile = tempFile;
        _preparingPreview = null;

        RunFocusMonitor();

        BeginShowNewWindow(tempFile, matchedPlugin);
    }

    private void ReportCopyProgress(long requestId, long copiedBytes, long expectedBytes)
    {
        if (requestId != Interlocked.Read(ref _previewRequestId))
            return;

        var preparing = _preparingPreview;

        if (preparing == null)
            return;

        var status = expectedBytes > 0
            ? $"Preparing preview… {copiedBytes.ToPrettySize(2)} of {expectedBytes.ToPrettySize(2)}"
            : $"Preparing preview… {copiedBytes.ToPrettySize(2)}";

        var dispatcher = Application.Current?.Dispatcher;

        dispatcher?.BeginInvoke(new Action(() =>
        {
            if (requestId == Interlocked.Read(ref _previewRequestId)
             && _preparingPreview == preparing
             && _viewerWindow.IsVisible)
            {
                preparing.SetStatus(status);
            }
        }));
    }

    /// <summary>
    ///     Applies the result of the background preparation on the UI thread, but only when this request is
    ///     still the latest one and the window is still open.
    /// </summary>
    private void PostCompletion(long requestId, string shellPath, string tempFile)
    {
        var dispatcher = Application.Current?.Dispatcher;

        if (dispatcher == null)
            return;

        dispatcher.BeginInvoke(new Action(() =>
        {
            if (requestId != Interlocked.Read(ref _previewRequestId) || !_viewerWindow.IsVisible)
            {
                // A newer preview was requested (or the window was closed) while this copy was running.
                if (!string.IsNullOrEmpty(tempFile))
                    ScheduleTempFileCleanup(tempFile);

                return;
            }

            CompleteMaterialization(shellPath, tempFile);
        }));
    }

    /// <summary>
    ///     Updates the placeholder title/name once the background resolution knows the shell display name.
    /// </summary>
    private void UpdatePreparingInfo(long requestId, string displayName)
    {
        if (requestId != Interlocked.Read(ref _previewRequestId))
            return;

        var dispatcher = Application.Current?.Dispatcher;

        dispatcher?.BeginInvoke(new Action(() =>
        {
            if (requestId != Interlocked.Read(ref _previewRequestId))
                return;

            var preparing = _preparingPreview;

            if (preparing == null)
                return;

            preparing.SetTitle(displayName);
            preparing.SetDisplayName(displayName);
        }));
    }

    /// <summary>
    ///     Cancels the shell item copy that is currently running, if any. The copy itself cannot always be
    ///     aborted inside the shell, but its result is discarded and the temporary file is cleaned up.
    /// </summary>
    private void CancelPendingMaterialization()
    {
        // Invalidate any completion that is already queued on the dispatcher.
        Interlocked.Increment(ref _previewRequestId);

        var cancellation = _materializeCts;

        if (cancellation == null)
            return;

        _materializeCts = null;

        try
        {
            cancellation.Cancel();
        }
        catch (Exception)
        {
            // ignored
        }

        cancellation.Dispose();
    }

    /// <summary>
    ///     Drops the temporary copy owned by the preview that is being replaced. The previous plugin may
    ///     still be reading that file asynchronously, so removal is deferred.
    /// </summary>
    private void ReleasePreviousMaterializedFile()
    {
        var previousTempFile = _materializedTempFile;
        _materializedTempFile = null;

        if (!string.IsNullOrEmpty(previousTempFile))
            ScheduleTempFileCleanup(previousTempFile);
    }

    private static void ScheduleTempFileCleanup(string tempFile)
    {
        if (string.IsNullOrEmpty(tempFile))
            return;

        _ = Task.Delay(TimeSpan.FromSeconds(20))
            .ContinueWith(_ => ShellItemHelper.TryDeleteTempFile(tempFile), TaskScheduler.Default);
    }

    /// <summary>
    ///     Creates a fresh instance of the default plugin for every preview. Reusing the single instance
    ///     stored in <see cref="PluginManager.DefaultPlugin" /> would let a closing pinned window stop the
    ///     info panel of a newer preview.
    /// </summary>
    private static IViewer CreateDefaultPluginInstance()
    {
        var defaultPlugin = PluginManager.GetInstance().DefaultPlugin;

        return defaultPlugin.GetType().CreateInstance<IViewer>();
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
            // Only replace the pending preparation once a valid plugin preview is ready to be shown.
            CancelPendingMaterialization();
            BeginShowNewWindow(path, matchedPlugin);
        }
    }

    public void ReloadPreview()
    {
        if (!_viewerWindow.IsVisible || string.IsNullOrEmpty(_invokedPath))
            return;

        CancelPendingMaterialization();

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
            BeginShowNewWindow(path, CreateDefaultPluginInstance());
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
            CancelPendingMaterialization();
            _preparingPreview = null;
            StopFocusMonitor();
            InitNewViewerWindow();
        };
    }

    public static ViewWindowManager GetInstance()
    {
        return _instance ??= new ViewWindowManager();
    }
}
