using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using QuickLook.ExtensionMethods;
using QuickLook.Helpers;
using QuickLook.Plugin;

namespace QuickLook
{
    internal class ViewWindowManager
    {
        private static ViewWindowManager _instance;

        private MainWindow _viewWindow;

        internal void InvokeViewer(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            if (!Directory.Exists(path) && !File.Exists(path))
                return;

            var matchedPlugin = PluginManager.GetInstance().FindMatch(path);

            BeginShowNewWindow(matchedPlugin, path);
        }

        internal void InvokeRoutine()
        {
            if (!WindowHelper.IsFocusedControlExplorerItem())
                if (!WindowHelper.IsFocusedWindowSelf())
                    return;

            if (CloseCurrentWindow())
                return;

            var path = GetCurrentSelection();

            InvokeViewer(path);
        }

        private void BeginShowNewWindow(IViewer matchedPlugin, string path)
        {
            _viewWindow = new MainWindow();
            _viewWindow.Closed += (sender2, e2) =>
            {
                if (App.RunningAsViewer)
                {
                    Application.Current.Shutdown();
                    return;
                }

                _viewWindow.Dispose();
                _viewWindow = null;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            };

            try
            {
                _viewWindow.BeginShow(matchedPlugin, path);
            }
            catch (Exception e) // if current plugin failed, switch to default one.
            {
                _viewWindow.Close();

                Debug.WriteLine(e.ToString());
                Debug.WriteLine(e.StackTrace);

                if (matchedPlugin.GetType() != PluginManager.GetInstance().DefaultPlugin)
                {
                    matchedPlugin.Dispose();
                    matchedPlugin = PluginManager.GetInstance().DefaultPlugin.CreateInstance<IViewer>();
                    BeginShowNewWindow(matchedPlugin, path);
                }
                else
                {
                    throw;
                }
            }
#pragma warning disable 1058
            catch // Catch SEH exceptions here.
#pragma warning restore 1058
            {
                _viewWindow.Close();

                if (matchedPlugin.GetType() != PluginManager.GetInstance().DefaultPlugin)
                {
                    matchedPlugin.Dispose();
                    matchedPlugin = PluginManager.GetInstance().DefaultPlugin.CreateInstance<IViewer>();
                    BeginShowNewWindow(matchedPlugin, path);
                }
                else
                {
                    throw;
                }
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