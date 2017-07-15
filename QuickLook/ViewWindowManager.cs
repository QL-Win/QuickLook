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
using System.Windows.Forms;
using System.Windows.Threading;
using QuickLook.Helpers;
using QuickLook.Plugin;

namespace QuickLook
{
    internal class ViewWindowManager : IDisposable
    {
        private static ViewWindowManager _instance;

        private MainWindowNoTransparent _viewWindowNoTransparent;
        private MainWindowTransparent _viewWindowTransparent;
        private MainWindowTransparent _currentMainWindow;

        private string _path = string.Empty;

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

        internal void InvokeRoutine(KeyEventArgs kea, bool isKeyDown)
        {
            Debug.WriteLine($"InvokeRoutine: key={kea.KeyCode},down={isKeyDown}");


            if (isKeyDown)
                switch (kea.KeyCode)
                {
                    case Keys.Enter:
                        RunAndClosePreview();
                        break;
                }
            else
                switch (kea.KeyCode)
                {
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Left:
                    case Keys.Right:
                        SwitchPreview();
                        break;
                    case Keys.Space:
                        TogglePreview();
                        break;
                    case Keys.Escape:
                        ClosePreview();
                        break;
                }
        }

        internal void RunAndClosePreview()
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

        internal void ClosePreview()
        {
            if (_currentMainWindow.Visibility != Visibility.Visible)
                return;

            StopFocusMonitor();
            _currentMainWindow.BeginHide();
        }

        private void TogglePreview()
        {
            if (_currentMainWindow.Visibility == Visibility.Visible)
            {
                ClosePreview();
            }
            else
            {
                _path = NativeMethods.QuickLook.GetCurrentSelection();
                InvokeViewer();
            }
        }

        private void SwitchPreview()
        {
            if (_currentMainWindow.Visibility != Visibility.Visible)
                return;

            // if the switch has been done by SwitchPreviewRemoteInvoke, we'll not do anything
            var select = NativeMethods.QuickLook.GetCurrentSelection();
            if (_path == select)
                return;

            _path = select;

            Debug.WriteLine($"SwitchPreview: {_path}");

            InvokeViewer();
        }

        private void SwitchPreviewRemoteInvoke(HeartbeatEventArgs e)
        {
            // if the switch has been done by SwitchPreview, we'll not do anything
            if (e.FocusedFile == _path)
                return;

            Debug.WriteLine($"SwitchPreviewRemoteInvoke: {e.FocusedFile}");

            if (string.IsNullOrEmpty(e.FocusedFile))
                return;

            _currentMainWindow?.Dispatcher.BeginInvoke(new Action(SwitchPreview), DispatcherPriority.ApplicationIdle);
        }

        private void RunFocusMonitor()
        {
            if (!FocusMonitor.GetInstance().IsRunning)
            {
                FocusMonitor.GetInstance().Start();
                FocusMonitor.GetInstance().Heartbeat += SwitchPreviewRemoteInvoke;
            }
        }

        private void StopFocusMonitor()
        {
            if (FocusMonitor.GetInstance().IsRunning)
            {
                FocusMonitor.GetInstance().Stop();
                FocusMonitor.GetInstance().Heartbeat -= SwitchPreviewRemoteInvoke;
            }
        }
        
        internal void ForgetCurrentWindow()
        {
            StopFocusMonitor();

            if (ReferenceEquals(_currentMainWindow, _viewWindowTransparent))
                _viewWindowTransparent=new MainWindowTransparent();
            else
                _viewWindowNoTransparent = new MainWindowNoTransparent();

            _currentMainWindow = _viewWindowTransparent;
        }

        internal bool InvokeViewer(string path = null, bool closeIfSame = false)
        {
            if(closeIfSame)
                if (_currentMainWindow.Visibility == Visibility.Visible && path == _path)
                {
                    ClosePreview();
                    return false;
                }

            if (path != null)
                _path = path;

            if (string.IsNullOrEmpty(_path))
                return false;
            if (!Directory.Exists(_path) && !File.Exists(_path))
                return false;

            RunFocusMonitor();

            var matchedPlugin = PluginManager.GetInstance().FindMatch(_path);

            BeginShowNewWindow(matchedPlugin);

            return true;
        }

        private void BeginShowNewWindow(IViewer matchedPlugin)
        {
            _currentMainWindow.UnloadPlugin();

            // switch window
            var oldWindow = _currentMainWindow;
            _currentMainWindow = matchedPlugin.AllowsTransparency
                ? _viewWindowTransparent
                : _viewWindowNoTransparent;
            if (!ReferenceEquals(oldWindow, _currentMainWindow))
                oldWindow.BeginHide();
            
            _currentMainWindow.BeginShow(matchedPlugin, _path, CurrentPluginFailed);
        }

        private void CurrentPluginFailed(ExceptionDispatchInfo e)
        {
            var plugin = _currentMainWindow.Plugin.GetType();

            _currentMainWindow.BeginHide();

            TrayIconManager.GetInstance().ShowNotification("", $"Failed to preview {Path.GetFileName(_path)}", true);

            Debug.WriteLine(e.SourceException.ToString());
            Debug.WriteLine(e.SourceException.StackTrace);

            if (plugin != PluginManager.GetInstance().DefaultPlugin.GetType())
                BeginShowNewWindow(PluginManager.GetInstance().DefaultPlugin);
            else
                e.Throw();
        }

        internal static ViewWindowManager GetInstance()
        {
            return _instance ?? (_instance = new ViewWindowManager());
        }
    }
}