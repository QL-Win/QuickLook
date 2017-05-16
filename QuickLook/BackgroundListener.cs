using System;
using System.Windows.Forms;

namespace QuickLook
{
    internal class BackgroundListener : IDisposable
    {
        private static BackgroundListener _instance;

        private GlobalKeyboardHook _hook;

        protected BackgroundListener()
        {
            InstallHook(HotkeyEventHandler);
        }

        public void Dispose()
        {
            _hook?.Dispose();
        }

        private void HotkeyEventHandler(object sender, KeyEventArgs e)
        {
            if (e.Modifiers != Keys.None)
                return;

            ViewWindowManager.GetInstance().InvokeRoutine(e.KeyCode != Keys.Space);
        }

        private void InstallHook(KeyEventHandler handler)
        {
            _hook = GlobalKeyboardHook.GetInstance();

            _hook.HookedKeys.Add(Keys.Space);
            _hook.HookedKeys.Add(Keys.Up);
            _hook.HookedKeys.Add(Keys.Down);
            _hook.HookedKeys.Add(Keys.Left);
            _hook.HookedKeys.Add(Keys.Right);

            _hook.KeyUp += handler;
        }

        internal static BackgroundListener GetInstance()
        {
            return _instance ?? (_instance = new BackgroundListener());
        }
    }
}