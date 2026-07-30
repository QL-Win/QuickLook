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
using System.Windows.Threading;

namespace QuickLook;

public class ViewWindowManager : IDisposable
{
    private static ViewWindowManager _instance;

    private const int LoadingDelayMs = 150;
    private const int SlowLoadingMs = 12000;
    private const int TimeoutLoadingMs = 45000;

    private string _invokedPath = string.Empty;
    private DispatcherTimer _loadingDelayTimer;
    private DispatcherTimer _slowLoadingTimer;
    private DispatcherTimer _timeoutLoadingTimer;
    private int _previewRequestId;
    private ViewerWindow _viewerWindow;

    internal ViewWindowManager()
    {
        InitNewViewerWindow();
    }

    public void Dispose()
    {
        StopLoadingTimers();
        StopFocusMonitor();
    }

    public void RunAndClosePreview()
    {
        if (!_viewerWindow.IsVisible)
            return;

        _previewRequestId++;

        // if the current focus is in Desktop or explorer windows, just close the preview window and leave the task to System.
        var focus = NativeMethods.QuickLook.GetFocusedWindowType();
        if (focus != NativeMethods.QuickLook.FocusedWindowType.Invalid)
        {
            StopLoadingTimers();
            StopFocusMonitor();
            _viewerWindow.Close();
            return;
        }

        // if the focus is in the preview window, run it
        if (!WindowHelper.IsForegroundWindowBelongToSelf())
            return;

        _previewRequestId++;
        StopLoadingTimers();

        StopFocusMonitor();
        _viewerWindow.RunAndClose();
    }

    public void ClosePreview()
    {
        if (!_viewerWindow.IsVisible)
            return;

        _previewRequestId++;
        StopLoadingTimers();

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
        StopLoadingTimers();
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
            if (!path.StartsWith("::")) // CLSID
                return;

        // Check extension filtering before proceeding (skip for directories)
        if (!isDirectory && !ExtensionFilterHelper.IsExtensionAllowed(path))
            return;

        _invokedPath = path;

        RunFocusMonitor();

        var requestId = ++_previewRequestId;

        BeginPreviewRequest(path, requestId, () => PluginManager.GetInstance().FindMatch(path));
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

        _invokedPath = path;

        RunFocusMonitor();

        var requestId = ++_previewRequestId;

        BeginPreviewRequest(path, requestId, () => PluginManager.GetInstance().LoadedPlugins.Find(p =>
            p.GetType().Assembly.GetName().Name == plugin));
    }

    public void ReloadPreview()
    {
        if (!_viewerWindow.IsVisible || string.IsNullOrEmpty(_invokedPath))
            return;

        var path = _invokedPath;
        var requestId = ++_previewRequestId;

        BeginPreviewRequest(path, requestId, () => PluginManager.GetInstance().FindMatch(path), true);
    }

    public void ToggleFullscreen()
    {
        if (!_viewerWindow.IsVisible)
            return;

        _viewerWindow.ToggleFullscreen();
    }

    private void BeginShowNewWindow(string path, IViewer matchedPlugin)
    {
        var requestId = ++_previewRequestId;

        BeginPreviewRequest(path, requestId, () => matchedPlugin, true);
    }

    private void BeginPreviewRequest(string path, int requestId, Func<IViewer> pluginFactory, bool showImmediately = false)
    {
        StopLoadingTimers();

        var status = CreateLoadingStatus(path);
        var shouldShowImmediately = showImmediately || _viewerWindow.IsVisible || status.ShowImmediately;

        _viewerWindow.UnloadPlugin();
        ScheduleLoadingStatus(path, requestId, status, shouldShowImmediately);
        ScheduleLongRunningStatus(path, requestId, status);

        Task.Run(pluginFactory).ContinueWith(task =>
        {
            _viewerWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsCurrentPreviewRequest(path, requestId))
                    return;

                StopLoadingDelay();

                if (task.IsFaulted)
                {
                    var exception = task.Exception?.GetBaseException() ?? new InvalidOperationException("Failed to prepare preview.");
                    CurrentPluginFailed(path, ExceptionDispatchInfo.Capture(exception));
                    return;
                }

                var matchedPlugin = task.Result;
                if (matchedPlugin == null)
                    return;

                _viewerWindow.UpdateLoadingStatus(
                    TranslationHelper.Get("MW_OpeningPreview", failsafe: "Opening preview..."),
                    TranslationHelper.Get("MW_OpeningPreviewDetail", failsafe: "Preparing the viewer."),
                    "\xe8e5");
                _viewerWindow.BeginShow(matchedPlugin, path, CurrentPluginFailed);
            }), DispatcherPriority.ContextIdle);
        });
    }

    private bool IsCurrentPreviewRequest(string path, int requestId)
    {
        return requestId == _previewRequestId &&
               string.Equals(path, _invokedPath, StringComparison.Ordinal);
    }

    private void ScheduleLoadingStatus(string path, int requestId, LoadingStatus status, bool showImmediately)
    {
        if (showImmediately)
        {
            ShowLoadingStatus(path, requestId, status);
            return;
        }

        _loadingDelayTimer = new DispatcherTimer(DispatcherPriority.Background, _viewerWindow.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(LoadingDelayMs),
        };
        _loadingDelayTimer.Tick += (_, _) =>
        {
            StopLoadingDelay();
            ShowLoadingStatus(path, requestId, status);
        };
        _loadingDelayTimer.Start();
    }

    private void ShowLoadingStatus(string path, int requestId, LoadingStatus status)
    {
        if (!IsCurrentPreviewRequest(path, requestId))
            return;

        _viewerWindow.BeginLoading(path, status.Text, status.DetailText, status.Glyph);
    }

    private void ScheduleLongRunningStatus(string path, int requestId, LoadingStatus initialStatus)
    {
        _slowLoadingTimer = new DispatcherTimer(DispatcherPriority.Background, _viewerWindow.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(SlowLoadingMs),
        };
        _slowLoadingTimer.Tick += (_, _) =>
        {
            StopTimer(ref _slowLoadingTimer);

            if (!ShouldUpdateLongRunningStatus(path, requestId))
                return;

            _viewerWindow.UpdateLoadingStatus(
                TranslationHelper.Get("MW_StillWaiting", failsafe: "Still waiting for the file..."),
                initialStatus.IsCloud
                    ? TranslationHelper.Get("MW_StillWaitingCloudDetail", failsafe: "The sync provider is still making it available.")
                    : TranslationHelper.Get("MW_StillWaitingDetail", failsafe: "The selected viewer is still preparing the preview."),
                initialStatus.IsCloud ? "\xebd3" : "\xe8a5");
        };
        _slowLoadingTimer.Start();

        _timeoutLoadingTimer = new DispatcherTimer(DispatcherPriority.Background, _viewerWindow.Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(TimeoutLoadingMs),
        };
        _timeoutLoadingTimer.Tick += (_, _) =>
        {
            StopTimer(ref _timeoutLoadingTimer);

            if (!ShouldUpdateLongRunningStatus(path, requestId))
                return;

            _viewerWindow.UpdateLoadingStatus(
                TranslationHelper.Get("MW_PreviewTakingLong", failsafe: "This is taking longer than expected."),
                initialStatus.IsCloud
                    ? TranslationHelper.Get("MW_PreviewTakingLongCloudDetail", failsafe: "Check your sync provider if the file does not finish downloading.")
                    : TranslationHelper.Get("MW_PreviewTakingLongDetail", failsafe: "QuickLook is still working on this preview."),
                "\xea84");
        };
        _timeoutLoadingTimer.Start();
    }

    private bool ShouldUpdateLongRunningStatus(string path, int requestId)
    {
        return IsCurrentPreviewRequest(path, requestId) &&
               _viewerWindow.IsVisible &&
               _viewerWindow.ContextObject.IsBusy;
    }

    private void StopLoadingTimers()
    {
        StopLoadingDelay();
        StopTimer(ref _slowLoadingTimer);
        StopTimer(ref _timeoutLoadingTimer);
    }

    private void StopLoadingDelay()
    {
        StopTimer(ref _loadingDelayTimer);
    }

    private static void StopTimer(ref DispatcherTimer timer)
    {
        timer?.Stop();
        timer = null;
    }

    private static LoadingStatus CreateLoadingStatus(string path)
    {
        var cloudInfo = CloudFileHelper.GetInfo(path);
        if (cloudInfo.IsPlaceholder)
        {
            var provider = string.IsNullOrWhiteSpace(cloudInfo.ProviderName)
                ? TranslationHelper.Get("MW_CloudProvider", failsafe: "cloud")
                : cloudInfo.ProviderName;

            return new LoadingStatus(
                string.Format(TranslationHelper.Get("MW_DownloadingFromProvider", failsafe: "Downloading from {0}..."), provider),
                TranslationHelper.Get("MW_DownloadingFromProviderDetail", failsafe: "The file must be available locally before preview opens."),
                "\xebd3",
                true,
                true);
        }

        return new LoadingStatus(
            TranslationHelper.Get("MW_PreparingPreview", failsafe: "Preparing preview..."),
            TranslationHelper.Get("MW_PreparingPreviewDetail", failsafe: "Selecting the best viewer."),
            "\xe8a5",
            false,
            false);
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

    private sealed class LoadingStatus
    {
        internal LoadingStatus(string text, string detailText, string glyph, bool isCloud, bool showImmediately)
        {
            Text = text;
            DetailText = detailText;
            Glyph = glyph;
            IsCloud = isCloud;
            ShowImmediately = showImmediately;
        }

        internal string Text { get; }

        internal string DetailText { get; }

        internal string Glyph { get; }

        internal bool IsCloud { get; }

        internal bool ShowImmediately { get; }
    }

    public static ViewWindowManager GetInstance()
    {
        return _instance ??= new ViewWindowManager();
    }
}
