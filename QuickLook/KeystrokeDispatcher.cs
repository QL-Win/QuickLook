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
        private bool _isPreviewRequest;
        private bool _spaceIsDown;
        private long _spaceHoldTick;
        private long _lastInvalidKeyPressTick;

        private const long HOLD_TO_PREVIEW_DURATION = TimeSpan.TicksPerMillisecond * 750;
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
            // skip invalid keys, but record the timestamp
            if (!_validKeys.Contains(e.KeyCode))
            {
                Debug.WriteLine($"Invalid keypress: key={e.KeyCode},down={isKeyDown}, time={_lastInvalidKeyPressTick}");
                _lastInvalidKeyPressTick = DateTime.Now.Ticks;
                return;
            }

            // skip valid keys when modifiers are used
            if (isKeyDown && e.Modifiers != Keys.None)
                return;

            // skip if key is valid but too close after pressing an invalid key
            if (DateTime.Now.Ticks - _lastInvalidKeyPressTick < VALID_KEY_PRESS_DELAY)
                return;
            _lastInvalidKeyPressTick = 0L;

            // skip if user is holding Space (don't skip other valid keys)
            if (isKeyDown && e.KeyCode == Keys.Space)
            {
                if (_spaceIsDown)
                    return;
                _spaceIsDown = true;
                _spaceHoldTick = DateTime.Now.Ticks;
            }

            // check if the valid key is a preview request
            if (isKeyDown)
            {
                _isPreviewRequest = NativeMethods.QuickLook.GetFocusedWindowType() !=
                                    NativeMethods.QuickLook.FocusedWindowType.Invalid;
                _isPreviewRequest |= WindowHelper.IsForegroundWindowBelongToSelf();
            } // else (when isKeyDown is false), _isPreviewRequest retain its current state

            // call InvokeRoutine only when user pressed a key in a valid window, or
            // released a key which was pressed in a valid window, with an exception of Space which
            // must be hold for 750ms before releasing.
            if (_isPreviewRequest)
            {
                if (isKeyDown || e.KeyCode != Keys.Space ||
                    DateTime.Now.Ticks - _spaceHoldTick >= HOLD_TO_PREVIEW_DURATION)
                    InvokeRoutine(e.KeyCode, isKeyDown);
            }

            // when the key has been released, reset variables
            if (!isKeyDown)
            {
                _isPreviewRequest = false;
                _spaceIsDown = e.KeyCode != Keys.Space && _spaceIsDown;
            }
        }

        private void InvokeRoutine(Keys key, bool isKeyDown)
        {
            Debug.WriteLine($"InvokeRoutine: key={key},down={isKeyDown}");

            if (isKeyDown)
            {
                switch (key)
                {
                    case Keys.Enter:
                        PipeServerManager.SendMessage(PipeMessages.RunAndClose);
                        break;
                    case Keys.Space:
                        PipeServerManager.SendMessage(PipeMessages.Toggle);
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
                    case Keys.Escape:
                        PipeServerManager.SendMessage(PipeMessages.Close);
                        break;
                    case Keys.Space:
                        PipeServerManager.SendMessage(PipeMessages.Toggle);
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