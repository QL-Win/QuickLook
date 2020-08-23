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
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using QuickLook.Common.Helpers;
using QuickLook.Helpers;

namespace QuickLook
{
    internal class KeystrokeDispatcher : IDisposable
    {
        private static KeystrokeDispatcher _instance;

        private static HashSet<Keys> _validKeys;

        private GlobalKeyboardHook _hook;
        private bool _isKeyDownInDesktopOrShell;
        private long _lastInvalidKeyPressTick;

        private const long VALID_KEY_PRESS_DELAY = TimeSpan.TicksPerSecond * 1;

        protected KeystrokeDispatcher()
        {
            InstallKeyHook(KeyDownEventHandler, KeyUpEventHandler);

            _validKeys = new HashSet<Keys>(new[]
            {
                Keys.Up, Keys.Down, Keys.Left, Keys.Right,
                Keys.Enter, Keys.Space, Keys.Escape
            });
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

            // check if the window is valid at the time of pressing a key, used for case 1
            if (isKeyDown)
            {
                _isKeyDownInDesktopOrShell = NativeMethods.QuickLook.GetFocusedWindowType() !=
                                             NativeMethods.QuickLook.FocusedWindowType.Invalid;

                _isKeyDownInDesktopOrShell |= WindowHelper.IsForegroundWindowBelongToSelf();
            }

            // call InvokeRoutine only when:
            // (1) user released a key which was pressed in a valid window, or
            // (2) user pressed a key in a valid window
            if (_isKeyDownInDesktopOrShell)
                InvokeRoutine(e.KeyCode, isKeyDown);

            // in case 2, reset the variable
            if (!isKeyDown)
                _isKeyDownInDesktopOrShell = false;
        }

        private void InvokeRoutine(Keys key, bool isKeyDown)
        {
            if (!_validKeys.Contains(key))
            {
                Debug.WriteLine($"Invalid keypress: key={key},down={isKeyDown}, time={_lastInvalidKeyPressTick}");

                _lastInvalidKeyPressTick = DateTime.Now.Ticks;
                return;
            }

            if (DateTime.Now.Ticks - _lastInvalidKeyPressTick < VALID_KEY_PRESS_DELAY)
                return;

            _lastInvalidKeyPressTick = 0L;

            Debug.WriteLine($"InvokeRoutine: key={key},down={isKeyDown}");

            if (isKeyDown)
            {
                switch (key)
                {
                    case Keys.Enter:
                        PipeServerManager.SendMessage(PipeMessages.RunAndClose);
                        break;
                }
            }
            else
            {
                switch (key)
                {
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Left:
                    case Keys.Right:
                        PipeServerManager.SendMessage(PipeMessages.Switch);
                        break;
                    case Keys.Space:
                        PipeServerManager.SendMessage(PipeMessages.Toggle);
                        break;
                    case Keys.Escape:
                        PipeServerManager.SendMessage(PipeMessages.Close);
                        break;
                }
            }
        }

        private void InstallKeyHook(KeyEventHandler downHandler, KeyEventHandler upHandler)
        {
            _hook = GlobalKeyboardHook.GetInstance();

            _hook.KeyDown += downHandler;
            _hook.KeyUp += upHandler;
        }

        internal static KeystrokeDispatcher GetInstance()
        {
            return _instance ?? (_instance = new KeystrokeDispatcher());
        }
    }
}