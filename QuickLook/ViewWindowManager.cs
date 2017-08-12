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
using QuickLook.Helpers;
using QuickLook.Plugin;

namespace QuickLook
{
    internal class ViewWindowManager : IDisposable
    {
        private static ViewWindowManager _instance;
        private MainWindowTransparent _currentMainWindow;

        private string _invokedPath = string.Empty;

        private MainWindowNoTransparent _viewWindowNoTransparent;
        private MainWindowTransparent _viewWindowTransparent;

        internal ViewWindowManager()
        {
            _viewWindowTransparent = new MainWindowTransparent();
            _viewWindowNoTransparent = new MainWindowNoTransparent();

            _currentMainWindow = _viewWindowTransparent;
        }

        public void Dispose()
        {
            StopFocusMonitor();
            ClosePreview();
        }

        public void RunAndClosePreview()
        {
            if (_currentMainWindow.Visibility != Visibility.Visible)
                return;

            // if the current focus is in Desktop or explorer windows, just close the preview window and leave the task to System.
            var focus = NativeMethods.QuickLook.GetFocusedWindowType();
            if (focus != NativeMethods.QuickLook.FocusedWindowType.Invalid)
            {
                StopFocusMonitor();
                _currentMainWindow.BeginHide();
                return;
            }

            // if the focus is in the preview window, run it
            if (!WindowHelper.IsForegroundWindowBelongToSelf())
                return;

            StopFocusMonitor();
            _currentMainWindow.RunAndHide();
        }

        public void ClosePreview()
        {
            if (_currentMainWindow.Visibility != Visibility.Visible)
                return;

            StopFocusMonitor();
            _currentMainWindow.BeginHide();
        }

        public void TogglePreview(string path)
        {
            if (_currentMainWindow.Visibility == Visibility.Visible)
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

            if (ReferenceEquals(_currentMainWindow, _viewWindowTransparent))
                _viewWindowTransparent = new MainWindowTransparent();
            else
                _viewWindowNoTransparent = new MainWindowNoTransparent();

            _currentMainWindow = _viewWindowTransparent;
        }

        public void InvokePreview(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (_currentMainWindow.Visibility == Visibility.Visible && path == _invokedPath)
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
            _currentMainWindow.UnloadPlugin();

            // switch window
            var oldWindow = _currentMainWindow;
            _currentMainWindow = matchedPlugin.AllowsTransparency
                ? _viewWindowTransparent
                : _viewWindowNoTransparent;
            if (!ReferenceEquals(oldWindow, _currentMainWindow))
                oldWindow.BeginHide();

            _currentMainWindow.BeginShow(matchedPlugin, path, CurrentPluginFailed);
        }

        private void CurrentPluginFailed(string path, ExceptionDispatchInfo e)
        {
            var plugin = _currentMainWindow.Plugin.GetType();

            _currentMainWindow.BeginHide();

            TrayIconManager.ShowNotification("", $"Failed to preview {Path.GetFileName(path)}", true);

            Debug.WriteLine(e.SourceException.ToString());
            Debug.WriteLine(e.SourceException.StackTrace);

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