// Copyright © 2017-2025 QL-Win Contributors
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

namespace QuickLook;

public class ViewWindowManager : IDisposable
{
    private static ViewWindowManager _instance;

    private string _invokedPath = string.Empty;
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

        if (!Directory.Exists(path) && !File.Exists(path))
            if (!path.StartsWith("::")) // CLSID
                return;

        // Check extension filtering before proceeding
        if (!ExtensionFilterHelper.IsExtensionAllowed(path))
            return;

        _invokedPath = path;

        RunFocusMonitor();

        var matchedPlugin = PluginManager.GetInstance().FindMatch(path);

        BeginShowNewWindow(path, matchedPlugin);
    }

    public void InvokePluginPreview(string plugin, string path = null)
    {
        if (string.IsNullOrEmpty(path))
            path = _invokedPath;

        if (string.IsNullOrEmpty(path))
            return;

        if (!Directory.Exists(path) && !File.Exists(path))
            return;

        // Check extension filtering before proceeding
        if (!ExtensionFilterHelper.IsExtensionAllowed(path))
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

        var matchedPlugin = PluginManager.GetInstance().FindMatch(_invokedPath);

        BeginShowNewWindow(_invokedPath, matchedPlugin);
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
            if (sender is not ViewerWindow w || w.Pinned)
                return; // Pinned window has already been forgotten
            StopFocusMonitor();
            InitNewViewerWindow();
        };
    }

    public static ViewWindowManager GetInstance()
    {
        return _instance ??= new ViewWindowManager();
    }
}
