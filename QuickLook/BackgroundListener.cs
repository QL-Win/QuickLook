// Copyright © 2017 Paddy Xu
// 
// This file is part of QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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