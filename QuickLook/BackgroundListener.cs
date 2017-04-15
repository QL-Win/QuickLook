using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using QuickLook.Utilities;

namespace QuickLook
{
    internal class BackgroundListener
    {
        private static BackgroundListener _instance;

        private GlobalKeyboardHook _hook;

        private MainWindow _showingWindow;

        protected BackgroundListener()
        {
            InstallHook(HotkeyEventHandler);
        }

        private void HotkeyEventHandler(object sender, KeyEventArgs e)
        {
            if (_showingWindow != null)
            {
                _showingWindow.Close();
                _showingWindow = null;

                GC.Collect();

                return;
            }

            var path = String.Empty;

            // communicate with COM in a separate thread
            Task.Run(() =>
            {
                var paths = GetCurrentSelection();

                if (paths.Any())
                    path = paths.First();

            }).Wait();

            if (String.IsNullOrEmpty(path))
                return;

            var matched = PluginManager.FindMatch(path);

            if (matched == null)
                return;

            _showingWindow = new MainWindow();

            _showingWindow.Closed += (sender2, e2) => { _showingWindow = null; };

            _showingWindow.viewContentContainer.ViewerPlugin = matched;
            matched.View(path, _showingWindow.viewContentContainer);

            _showingWindow.Show();

            _showingWindow.ShowFinishLoadingAnimation();
        }

        private void InstallHook(KeyEventHandler handler)
        {
            _hook = GlobalKeyboardHook.GetInstance();

            _hook.HookedKeys.Add(Keys.Space);

            _hook.KeyUp += handler;
        }

        private string[] GetCurrentSelection()
        {
            NativeMethods.QuickLook.SaveCurrentSelection();

            var n = NativeMethods.QuickLook.GetCurrentSelectionCount();

            var sb = new StringBuilder(n * 261); // MAX_PATH + NULL = 261

            NativeMethods.QuickLook.GetCurrentSelectionBuffer(sb);

            return sb.Length == 0 ? new string[0] : sb.ToString().Split('|');
        }

        internal static BackgroundListener GetInstance()
        {
            return _instance ?? (_instance = new BackgroundListener());
        }
    }
}