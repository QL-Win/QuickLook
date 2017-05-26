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
            InstallKeyHook(HotkeyEventHandler);
        }

        public void Dispose()
        {
            _hook?.Dispose();
        }

        private void HotkeyEventHandler(object sender, KeyEventArgs e)
        {
            if (e.Modifiers != Keys.None)
                return;

            ViewWindowManager.GetInstance().InvokeRoutine(e.KeyCode);
        }

        private void InstallKeyHook(KeyEventHandler handler)
        {
            _hook = GlobalKeyboardHook.GetInstance();

            _hook.HookedDownKeys.Add(Keys.Enter);
            _hook.KeyDown += handler;

            _hook.HookedUpKeys.Add(Keys.Space);
            _hook.HookedUpKeys.Add(Keys.Escape);
            _hook.HookedUpKeys.Add(Keys.Up);
            _hook.HookedUpKeys.Add(Keys.Down);
            _hook.HookedUpKeys.Add(Keys.Left);
            _hook.HookedUpKeys.Add(Keys.Right);
            _hook.KeyUp += handler;
        }

        internal static BackgroundListener GetInstance()
        {
            return _instance ?? (_instance = new BackgroundListener());
        }
    }
}