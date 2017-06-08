using System;
using System.Windows.Forms;
using System.Windows.Threading;
using QuickLook.Helpers;

namespace QuickLook
{
    internal class BackgroundListener : IDisposable
    {
        private static BackgroundListener _instance;

        private GlobalKeyboardHook _hook;
        private bool _isKeyDownInDesktopOrShell;

        protected BackgroundListener()
        {
            InstallKeyHook(KeyDownEventHandler, KeyUpEventHandler);
        }

        public void Dispose()
        {
            _hook?.Dispose();
            _hook = null;
        }

        private void KeyDownEventHandler(object sender, KeyEventArgs e)
        {
            CallViewWindowManagerInvokeRoutine(e, true);
        }

        private void KeyUpEventHandler(object sender, KeyEventArgs e)
        {
            CallViewWindowManagerInvokeRoutine(e, false);
        }

        private void CallViewWindowManagerInvokeRoutine(KeyEventArgs e, bool isKeyDown)
        {
            if (e.Modifiers != Keys.None)
                return;

            // set variable only when KeyDown
            if (isKeyDown)
            {
                _isKeyDownInDesktopOrShell = NativeMethods.QuickLook.GetFocusedWindowType() !=
                                             NativeMethods.QuickLook.FocusedWindowType.Invalid;
                _isKeyDownInDesktopOrShell |= WindowHelper.IsForegroundWindowBelongToSelf();
            }

            // call InvokeRoutine only when the KeyDown is valid
            if (_isKeyDownInDesktopOrShell)
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action<bool>(down =>
                        ViewWindowManager.GetInstance().InvokeRoutine(e, down)),
                    DispatcherPriority.ApplicationIdle,
                    isKeyDown);

            // reset variable only when KeyUp
            if (!isKeyDown)
                _isKeyDownInDesktopOrShell = false;
        }

        private void InstallKeyHook(KeyEventHandler downHandler, KeyEventHandler upHandler)
        {
            _hook = GlobalKeyboardHook.GetInstance();

            _hook.HookedKeys.Add(Keys.Enter);
            _hook.HookedKeys.Add(Keys.Space);
            _hook.HookedKeys.Add(Keys.Escape);
            _hook.HookedKeys.Add(Keys.Up);
            _hook.HookedKeys.Add(Keys.Down);
            _hook.HookedKeys.Add(Keys.Left);
            _hook.HookedKeys.Add(Keys.Right);
            _hook.KeyDown += downHandler;
            _hook.KeyUp += upHandler;
        }

        internal static BackgroundListener GetInstance()
        {
            return _instance ?? (_instance = new BackgroundListener());
        }
    }
}