using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Forms;
using QuickLook.Helpers;
using QuickLook.Plugin;

namespace QuickLook
{
    internal class ViewWindowManager : IDisposable
    {
        private static ViewWindowManager _instance;

        private readonly MainWindowNoTransparent _viewWindowNoTransparent;
        private readonly MainWindowTransparent _viewWindowTransparentTransparent;
        private MainWindowTransparent _currentMainWindow;
        private long _lastSwitchTick;

        private string _path = string.Empty;

        internal ViewWindowManager()
        {
            _viewWindowTransparentTransparent = new MainWindowTransparent();
            _viewWindowNoTransparent = new MainWindowNoTransparent();

            _currentMainWindow = _viewWindowTransparentTransparent;
        }

        public void Dispose()
        {
            StopFocusMonitor();
        }

        internal void InvokeRoutine(KeyEventArgs kea)
        {
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
                case Keys.Enter:
                    RunAndClosePreview();
                    break;
                default:
                    break;
            }
        }

        internal void RunAndClosePreview()
        {
            if (NativeMethods.QuickLook.GetFocusedWindowType() ==
                NativeMethods.QuickLook.FocusedWindowType.Invalid)
                if (!WindowHelper.IsForegroundWindowBelongToSelf())
                    return;

            if (_currentMainWindow.Visibility != Visibility.Visible)
                return;

            StopFocusMonitor();
            _currentMainWindow.RunAndHide();
        }

        internal void ClosePreview()
        {
            if (NativeMethods.QuickLook.GetFocusedWindowType() ==
                NativeMethods.QuickLook.FocusedWindowType.Invalid)
                if (!WindowHelper.IsForegroundWindowBelongToSelf())
                    return;

            if (_currentMainWindow.Visibility != Visibility.Visible)
                return;

            StopFocusMonitor();
            _currentMainWindow.BeginHide();
        }

        private void TogglePreview()
        {
            _lastSwitchTick = DateTime.Now.Ticks;

            if (NativeMethods.QuickLook.GetFocusedWindowType() ==
                NativeMethods.QuickLook.FocusedWindowType.Invalid)
                if (!WindowHelper.IsForegroundWindowBelongToSelf())
                    return;

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

            _lastSwitchTick = DateTime.Now.Ticks;

            if (NativeMethods.QuickLook.GetFocusedWindowType() ==
                NativeMethods.QuickLook.FocusedWindowType.Invalid)
                return;

            _path = NativeMethods.QuickLook.GetCurrentSelection();

            InvokeViewer();
        }

        private void SwitchPreviewRemoteInvoke(HeartbeatEventArgs e)
        {
            // sleep for 1.5s
            if (e.InvokeTick - _lastSwitchTick < 1.5 * TimeSpan.TicksPerSecond)
                return;

            if (e.FocusedFile == _path)
                return;

            Debug.WriteLine("SwitchPreviewRemoteInvoke:" + (e.InvokeTick - _lastSwitchTick));

            if (string.IsNullOrEmpty(e.FocusedFile))
                return;

            _currentMainWindow?.Dispatcher.Invoke(() => SwitchPreview());
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

        internal bool InvokeViewer(string path = null)
        {
            if (path == null)
                path = _path;

            if (string.IsNullOrEmpty(path))
                return false;
            if (!Directory.Exists(path) && !File.Exists(path))
                return false;

            RunFocusMonitor();

            var matchedPlugin = PluginManager.GetInstance().FindMatch(path);

            BeginShowNewWindow(matchedPlugin, path);

            return true;
        }

        private void BeginShowNewWindow(IViewer matchedPlugin, string path)
        {
            try
            {
                _currentMainWindow.UnloadPlugin();

                // switch window
                var oldWindow = _currentMainWindow;
                _currentMainWindow = matchedPlugin.AllowsTransparency
                    ? _viewWindowTransparentTransparent
                    : _viewWindowNoTransparent;
                if (!ReferenceEquals(oldWindow, _currentMainWindow))
                    oldWindow.BeginHide();

                _currentMainWindow.BeginShow(matchedPlugin, path);
            }
            catch (Exception e) // if current plugin failed, switch to default one.
            {
                _currentMainWindow.BeginHide();

                TrayIconManager.GetInstance().ShowNotification("", $"Failed to preview {Path.GetFileName(path)}", true);

                Debug.WriteLine(e.ToString());
                Debug.WriteLine(e.StackTrace);

                if (matchedPlugin != PluginManager.GetInstance().DefaultPlugin)
                {
                    matchedPlugin.Cleanup();
                    matchedPlugin = PluginManager.GetInstance().DefaultPlugin;
                    BeginShowNewWindow(matchedPlugin, path);
                }
                else
                {
                    ExceptionDispatchInfo.Capture(e).Throw();
                }
            }
        }

        internal static ViewWindowManager GetInstance()
        {
            return _instance ?? (_instance = new ViewWindowManager());
        }
    }
}