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
            ViewWindowManager.GetInstance().InvokeRoutine();
        }

        private void InstallHook(KeyEventHandler handler)
        {
            _hook = GlobalKeyboardHook.GetInstance();

            _hook.HookedKeys.Add(Keys.Space);

            _hook.KeyUp += handler;
        }

        internal static BackgroundListener GetInstance()
        {
            return _instance ?? (_instance = new BackgroundListener());
        }
    }
}