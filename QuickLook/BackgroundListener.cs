using System.Text;
using System.Threading;
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
            string[] paths;

            // communicate with COM in a separate thread
            var tCom = new Thread(() => paths = GetCurrentSelection());

            tCom.Start();
            tCom.Join();

            var mw = new MainWindow();
            mw.Show();
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