// Copyright © 2017 Paddy Xu
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

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;

namespace QuickLook
{
    internal class ViewWindowManager : IDisposable
    {
        private static ViewWindowManager _instance;

        private string _invokedPath = string.Empty;
        private ViewerWindow _viewerWindow;

        internal ViewWindowManager()
        {
            _viewerWindow = new ViewerWindow();
        }

        public void Dispose()
        {
            StopFocusMonitor();
        }

        public void RunAndClosePreview()
        {
            if (_viewerWindow.Visibility != Visibility.Visible)
                return;

            // if the current focus is in Desktop or explorer windows, just close the preview window and leave the task to System.
            var focus = NativeMethods.QuickLook.GetFocusedWindowType();
            if (focus != NativeMethods.QuickLook.FocusedWindowType.Invalid)
            {
                StopFocusMonitor();
                _viewerWindow.BeginHide();
                return;
            }

            // if the focus is in the preview window, run it
            if (!WindowHelper.IsForegroundWindowBelongToSelf())
                return;

            StopFocusMonitor();
            _viewerWindow.RunAndHide();
        }

        public void ClosePreview()
        {
            if (_viewerWindow.Visibility != Visibility.Visible)
                return;

            StopFocusMonitor();
            _viewerWindow.BeginHide();
        }

        public void TogglePreview(string path)
        {
            if (_viewerWindow.Visibility == Visibility.Visible && (string.IsNullOrEmpty(path) || path == _invokedPath))
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

            var newWindow = new ViewerWindow();

            /*if (_viewerWindow.WindowState != WindowState.Maximized)
            {
                newWindow.Top = _viewerWindow.Top;
                newWindow.Left = _viewerWindow.Left;
                newWindow.Width = _viewerWindow.Width;
                newWindow.Height = _viewerWindow.Height;
            }*/

            _viewerWindow = newWindow;
        }

        public void SwitchPreview(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (_viewerWindow.Visibility != Visibility.Visible)
                return;

            InvokePreview(path);
        }

        public void InvokePreview(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (_viewerWindow.Visibility == Visibility.Visible && path == _invokedPath)
                return;

            if (!Directory.Exists(path) && !File.Exists(path))
                return;

            _invokedPath = path;

            RunFocusMonitor();

            var matchedPlugin = PluginManager.GetInstance().FindMatch(path);

            BeginShowNewWindow(path, matchedPlugin);
        }

        private void BeginShowNewWindow(string path, IViewer matchedPlugin)
        {
            _viewerWindow.UnloadPlugin();

            _viewerWindow.BeginShow(matchedPlugin, path, CurrentPluginFailed);
        }

        private void CurrentPluginFailed(string path, ExceptionDispatchInfo e)
        {
            var plugin = _viewerWindow.Plugin.GetType();

            _viewerWindow.BeginHide();

            TrayIconManager.ShowNotification($"Failed to preview {Path.GetFileName(path)}",
                "Consider reporting this incident to QuickLook’s author.", true);

            Debug.WriteLine(e.SourceException.ToString());

            ProcessHelper.WriteLog(e.SourceException.ToString());

            if (plugin != PluginManager.GetInstance().DefaultPlugin.GetType())
                BeginShowNewWindow(path, PluginManager.GetInstance().DefaultPlugin);
            else
                e.Throw();
        }

        internal static ViewWindowManager GetInstance()
        {
            return _instance ?? (_instance = new ViewWindowManager());
        }
    }
}