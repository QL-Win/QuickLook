using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using QuickLook.Helpers;
using QuickLook.Plugin;

namespace QuickLook
{
    internal class ViewWindowManager
    {
        private static ViewWindowManager _instance;

        private readonly MainWindowNoTransparent _viewWindowNoTransparent;
        private readonly MainWindowTransparent _viewWindowTransparentTransparent;
        private MainWindowTransparent _currentMainWindow;

        private string _path = string.Empty;

        internal ViewWindowManager()
        {
            _viewWindowTransparentTransparent = new MainWindowTransparent();
            _viewWindowNoTransparent = new MainWindowNoTransparent();

            _currentMainWindow = _viewWindowTransparentTransparent;
        }

        internal void InvokeRoutine(KeyEventArgs kea)
        {
            Debug.WriteLine(kea.KeyCode);

            switch (kea.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    SwitchPreview(kea);
                    break;
                case Keys.Space:
                    TogglePreview(kea);
                    break;
                case Keys.Escape:
                    ClosePreview(kea);
                    break;
                case Keys.Enter:
                    RunAndClosePreview(kea);
                    break;
                default:
                    break;
            }
        }

        private void RunAndClosePreview(KeyEventArgs kea = null)
        {
            if (!WindowHelper.IsFocusedControlExplorerItem() && !WindowHelper.IsFocusedWindowSelf())
                return;

            if (_currentMainWindow.Visibility != Visibility.Visible)
                return;

            StopFocusMonitor();
            _currentMainWindow.RunAndClose();
            if (kea != null)
                kea.Handled = true;
        }

        internal void ClosePreview(KeyEventArgs kea = null)
        {
            StopFocusMonitor();
            _currentMainWindow.BeginHide();

            if (kea != null)
                kea.Handled = true;
        }

        private void TogglePreview(KeyEventArgs kea = null)
        {
            if (!WindowHelper.IsFocusedControlExplorerItem() && !WindowHelper.IsFocusedWindowSelf())
                return;

            if (_currentMainWindow.Visibility == Visibility.Visible)
            {
                ClosePreview();
            }
            else
            {
                _path = NativeMethods.QuickLook.GetCurrentSelectionFirst();
                InvokeViewer();
            }
            if (kea != null)
                kea.Handled = true;
        }

        private void SwitchPreview(KeyEventArgs kea = null)
        {
            if (_currentMainWindow.Visibility != Visibility.Visible)
                return;

            if (!WindowHelper.IsFocusedControlExplorerItem())
                return;

            _path = NativeMethods.QuickLook.GetCurrentSelectionFirst();

            InvokeViewer();
            if (kea != null)
                kea.Handled = false;
        }

        private void SwitchPreviewRemoteInvoke(FocusedItemChangedEventArgs e)
        {
            Debug.WriteLine("SwitchPreviewRemoteInvoke");

            if (e.FocusedFile == _path)
                return;

            if (string.IsNullOrEmpty(e.FocusedFile))
                return;

            _currentMainWindow?.Dispatcher.Invoke(() =>
            {
                if (_currentMainWindow.Visibility != Visibility.Visible)
                    return;

                if (!WindowHelper.IsFocusedControlExplorerItem())
                    return;

                _path = NativeMethods.QuickLook.GetCurrentSelectionFirst();

                InvokeViewer();
            });
        }

        private void RunFocusMonitor()
        {
            if (!FocusMonitor.GetInstance().IsRunning)
            {
                FocusMonitor.GetInstance().Start();
                FocusMonitor.GetInstance().FocusedItemChanged += SwitchPreviewRemoteInvoke;
            }
        }

        private void StopFocusMonitor()
        {
            if (FocusMonitor.GetInstance().IsRunning)
            {
                FocusMonitor.GetInstance().Stop();
                FocusMonitor.GetInstance().FocusedItemChanged -= SwitchPreviewRemoteInvoke;
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
                    throw;
                }
            }
        }

        internal static ViewWindowManager GetInstance()
        {
            return _instance ?? (_instance = new ViewWindowManager());
        }
    }
}