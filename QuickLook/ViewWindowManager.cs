using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    ClosePreview(kea, false);
                    break;
                case Keys.Enter:
                    ClosePreview(kea, true);
                    break;
                default:
                    break;
            }
        }

        private void ClosePreview(KeyEventArgs kea, bool runBeforeClose)
        {
            if (!WindowHelper.IsFocusedControlExplorerItem() && !WindowHelper.IsFocusedWindowSelf())
                return;

            if (_currentMainWindow.Visibility != Visibility.Visible)
                return;

            if (runBeforeClose)
                _currentMainWindow.RunAndClose();
            else
                _currentMainWindow.BeginHide();
            kea.Handled = true;
        }

        private void TogglePreview(KeyEventArgs kea)
        {
            if (!WindowHelper.IsFocusedControlExplorerItem() && !WindowHelper.IsFocusedWindowSelf())
                return;

            if (_currentMainWindow.Visibility == Visibility.Visible)
                _currentMainWindow.BeginHide();
            else
                InvokeViewer(GetCurrentSelection());
            kea.Handled = true;
        }

        private void SwitchPreview(KeyEventArgs kea)
        {
            if (_currentMainWindow.Visibility != Visibility.Visible)
                return;

            if (!WindowHelper.IsFocusedControlExplorerItem())
                return;

            _currentMainWindow.UnloadPlugin();
            InvokeViewer(GetCurrentSelection());
            kea.Handled = false;
        }

        internal void InvokeViewer(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            if (!Directory.Exists(path) && !File.Exists(path))
                return;

            var matchedPlugin = PluginManager.GetInstance().FindMatch(path);

            BeginShowNewWindow(matchedPlugin, path);
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

        private string GetCurrentSelection()
        {
            var path = string.Empty;

            // communicate with COM in a separate thread
            Task.Run(() =>
                {
                    var paths = GetCurrentSelectionNative();

                    if (paths.Any())
                        path = paths.First();
                })
                .Wait();

            return string.IsNullOrEmpty(path) ? string.Empty : path;
        }

        private string[] GetCurrentSelectionNative()
        {
            NativeMethods.QuickLook.SaveCurrentSelection();

            var n = NativeMethods.QuickLook.GetCurrentSelectionCount();
            var sb = new StringBuilder(n * 261); // MAX_PATH + NULL = 261
            NativeMethods.QuickLook.GetCurrentSelectionBuffer(sb);

            return sb.Length == 0 ? new string[0] : sb.ToString().Split('|');
        }

        internal static ViewWindowManager GetInstance()
        {
            return _instance ?? (_instance = new ViewWindowManager());
        }
    }
}