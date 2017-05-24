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

        internal void InvokeRoutine(Keys key)
        {
            // do we need switch to another file?
            var replaceView = key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right;

            if (replaceView && _currentMainWindow.Visibility != Visibility.Visible)
                return;

            if (!WindowHelper.IsFocusedControlExplorerItem())
                if (replaceView || !WindowHelper.IsFocusedWindowSelf())
                    return;

            // should the window be closed (replaceView == false), return without showing new one
            if (_currentMainWindow.BeginHide(disposePluginOnly: replaceView))
                return;

            var path = GetCurrentSelection();

            InvokeViewer(path);
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