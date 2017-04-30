using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickLook.ExtensionMethods;
using QuickLook.Helpers;
using QuickLook.Plugin;

namespace QuickLook
{
    internal class ViewWindowManager
    {
        private static ViewWindowManager _instance;

        private MainWindow _viewWindow;

        internal void InvokeRoutine()
        {
            if (CloseCurrentWindow())
                return;

            if (!WindowHelper.IsFocusedControlExplorerItem())
                return;

            var path = GetCurrentSelection();
            if (string.IsNullOrEmpty(path))
                return;

            var matchedPlugin = PluginManager.GetInstance().FindMatch(path);

            BeginShowNewWindow(matchedPlugin, path);
        }

        private void BeginShowNewWindow(IViewer matchedPlugin, string path)
        {
            _viewWindow = new MainWindow();
            _viewWindow.Closed += (sender2, e2) =>
            {
                _viewWindow.Dispose();
                _viewWindow = null;
                GC.Collect();
            };

            try
            {
                _viewWindow.BeginShow(matchedPlugin, path);
            }
            catch (Exception e) // if current plugin failed, switch to default one
            {
                Debug.WriteLine(e.ToString());
                Debug.WriteLine(e.StackTrace);

                if (matchedPlugin.GetType() != PluginManager.GetInstance().DefaultPlugin)
                    _viewWindow.BeginShow(PluginManager.GetInstance().DefaultPlugin.CreateInstance<IViewer>(), path);
            }
        }

        private bool CloseCurrentWindow()
        {
            if (_viewWindow != null)
            {
                _viewWindow.Close();

                return true;
            }
            return false;
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