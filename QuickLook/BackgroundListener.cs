using System;
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

        protected BackgroundListener()
        {
            InstallHook(HotkeyEventHandler);
        }

        private void HotkeyEventHandler(object sender, KeyEventArgs e)
        {
            var paths = new string[0];

            // communicate with COM in a separate thread
            Task.Run(() => paths = GetCurrentSelection()).Wait();

            var ddd = PathToPluginMatcher.FindMatch(paths);

            var mw = new MainWindow();

            ddd.View(paths[0], mw.ViewContentContainer);

            mw.Show();

            mw.ShowFinishLoadingAnimation(TimeSpan.FromMilliseconds(200));
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