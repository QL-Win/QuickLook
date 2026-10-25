// Copyright © 2017-2026 QL-Win Contributors
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

using QuickLook.Common.Helpers;
using QuickLook.Common.NativeMethods;
using QuickLook.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace QuickLook;

internal class KeystrokeDispatcher : IDisposable
{
    private static KeystrokeDispatcher _instance;

    private static HashSet<Keys> _validKeys;

    private GlobalKeyboardHook _hook;
    private nint _winEventHook;
    private User32.WinEventProc _winEventProc; // keep reference to prevent GC
    private readonly HashSet<Keys> _acceptedPresses = []; // keys whose current press started an action
    private bool _spaceIsDown;
    private long _spaceHoldTick;
    private int _spaceRepeatCount;
    private int _spaceReleasedChecks;
    private long _lastInvalidKeyPressTick;
    private long _lastSpacePressTick; // last accepted Space press, to tell a double tap
    private long _spaceSuppressedUntil; // after a double tap opened the editor, extra taps are ignored
    private Keys _lastInvalidKey; // for the log only: which key made a valid key count as "typing"
    private bool _lastInvalidKeyDown;
    private System.Windows.Forms.Timer _spaceWatchdog; // created and ticking on the keyboard hook thread

    private const long HOLD_TO_PREVIEW_DURATION = TimeSpan.TicksPerMillisecond * 750;
    private const long VALID_KEY_PRESS_DELAY = TimeSpan.TicksPerSecond * 1;
    private const int SPACE_WATCHDOG_INTERVAL = 100;

    // shorter than the double-click time, so two separate taps (open, then close) are not taken as one
    private const long DOUBLE_TAP_INTERVAL = TimeSpan.TicksPerMillisecond * 300;

    // a quick third tap after a double tap must not reopen the preview while the editor starts
    private const long AFTER_DOUBLE_TAP_QUIET = TimeSpan.TicksPerMillisecond * 1000;

    protected KeystrokeDispatcher()
    {
        // before the listener starts: its thread may deliver a key right away
        _validKeys =
        [
            Keys.Up, Keys.Down, Keys.Left, Keys.Right,
            Keys.Enter, Keys.Space, Keys.Escape,
            Keys.F5, Keys.F11,
        ];

        InstallKeyHook(KeyDownEventHandler, KeyUpEventHandler);

        // on the listener thread, so its callback never races with the key handlers
        _hook.Invoke(InstallForegroundWindowHook);
    }

    public void Dispose()
    {
        _hook?.Invoke(() =>
        {
            if (_winEventHook == IntPtr.Zero)
                return;

            User32.UnhookWinEvent(_winEventHook);
            _winEventHook = IntPtr.Zero;
        });

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
            // Keystrokes synthesized by other software (AutoHotkey, layout switchers, ...) are not the user
            // typing, and must not suppress QuickLook's keys. Only these are logged, never real typing.
            if (_hook?.IsCurrentEventInjected == true)
            {
                KeyboardLog.Write($"Injected {e.KeyCode} {(isKeyDown ? "down" : "up")}: not counted as typing");
                return;
            }

            Interlocked.Exchange(ref _lastInvalidKeyPressTick, DateTime.Now.Ticks);
            _lastInvalidKey = e.KeyCode;
            _lastInvalidKeyDown = isKeyDown;

            // modifiers reveal nothing about what is typed, and explain "right after typing" decisions
            if (IsModifierKey(e.KeyCode) && !(isKeyDown && _hook.IsCurrentEventRepeat))
                KeyboardLog.Write($"Modifier {e.KeyCode} {(isKeyDown ? "down" : "up")}");
            return;
        }

        var now = DateTime.Now.Ticks;

        // skip if user is holding Space (don't skip other valid keys)
        if (isKeyDown && e.KeyCode == Keys.Space)
        {
            if (_hook.IsCurrentEventRepeat)
            {
                _spaceRepeatCount++; // auto-repeat, not logged
                return;
            }

            // a new press although the previous one was never released
            if (_spaceIsDown)
                Log(e, isKeyDown, "previous key-up was lost, starting a new press");
        }

        if (isKeyDown)
            OnKeyPressed(e, now);
        else
            OnKeyReleased(e, now);
    }

    // Decides whether a press starts an action. Each key remembers the outcome, so its release completes
    // exactly that press, whatever other keys (or other software) did in between.
    private void OnKeyPressed(KeyEventArgs e, long now)
    {
        _acceptedPresses.Remove(e.KeyCode);

        if (e.KeyCode == Keys.Space)
        {
            // from now on its auto-repeat is swallowed, whether or not the press is accepted
            _spaceIsDown = true;
            _spaceHoldTick = now;
            _spaceRepeatCount = 0;
            StartSpaceWatchdog();
        }

        // skip valid keys when modifiers are used
        if (e.Modifiers != Keys.None)
        {
            Log(e, true, "ignored: modifier held");
            return;
        }

        if (e.KeyCode == Keys.Space && now < _spaceSuppressedUntil)
        {
            Log(e, true, "ignored: a double tap just opened the editor");
            return;
        }

        // skip if key is valid but too close after typing another key
        if (now - Interlocked.Read(ref _lastInvalidKeyPressTick) < VALID_KEY_PRESS_DELAY)
        {
            var ago = (now - Interlocked.Read(ref _lastInvalidKeyPressTick)) / TimeSpan.TicksPerMillisecond;
            Log(e, true, $"ignored: right after typing another key ({DescribeKey(_lastInvalidKey)} " +
                         $"{(_lastInvalidKeyDown ? "down" : "up")} {ago} ms before)");
            return;
        }
        Interlocked.Exchange(ref _lastInvalidKeyPressTick, 0L);

        // only act when a file view (or QuickLook itself) has the focus
        var focus = NativeMethods.QuickLook.GetFocusedWindowType();
        if (focus == NativeMethods.QuickLook.FocusedWindowType.Invalid && !WindowHelper.IsForegroundWindowBelongToSelf())
        {
            Log(e, true, $"ignored: focus is {focus}");
            return;
        }

        _acceptedPresses.Add(e.KeyCode);

        // a quick second Space edits the file instead of toggling the preview
        var doubleTap = false;
        if (e.KeyCode == Keys.Space)
        {
            doubleTap = now - _lastSpacePressTick <= DOUBLE_TAP_INTERVAL;
            _lastSpacePressTick = doubleTap ? 0L : now;
            if (doubleTap)
                _spaceSuppressedUntil = now + AFTER_DOUBLE_TAP_QUIET;
        }

        Log(e, true, doubleTap ? "invoke (double tap)" : "invoke");
        Dispatch(e.KeyCode, true, doubleTap);
    }

    private void OnKeyReleased(KeyEventArgs e, long now)
    {
        var accepted = _acceptedPresses.Remove(e.KeyCode);
        if (e.KeyCode == Keys.Space)
            _spaceIsDown = false;

        if (!accepted)
        {
            if (e.KeyCode == Keys.Space)
                Log(e, false, "no action: its press was not accepted");
            return;
        }

        // Space toggles on press; releasing it only closes again after a hold (hold-to-preview). Once the
        // keyboard auto-repeats, the user is holding the key, whatever their repeat delay setting is.
        if (e.KeyCode == Keys.Space && _spaceRepeatCount == 0 && now - _spaceHoldTick < HOLD_TO_PREVIEW_DURATION)
        {
            Log(e, false, "no action: short press");
            return;
        }

        Log(e, false, "invoke");
        Dispatch(e.KeyCode, false);
    }

    // Names the key behind a "right after typing" decision without revealing what was typed: keys that
    // produce text are only reported as such.
    private static string DescribeKey(Keys key)
    {
        var isText = key is >= Keys.A and <= Keys.Z or >= Keys.D0 and <= Keys.D9
            or >= Keys.NumPad0 and <= Keys.Divide
            or >= Keys.OemSemicolon and <= Keys.Oem102;
        return isText ? "a character key" : $"{key} (0x{(int)key:X2})";
    }

    private static bool IsModifierKey(Keys key)
    {
        return key is Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu
            or Keys.LWin or Keys.RWin;
    }

    private void Dispatch(Keys key, bool isKeyDown, bool doubleTap = false)
    {
        // we are on the keyboard hook thread; InvokeRoutine reads settings, which is UI-thread only
        Application.Current.Dispatcher.BeginInvoke(new Action(() => InvokeRoutine(key, isKeyDown, doubleTap)));
    }

    private void Log(KeyEventArgs e, bool isKeyDown, string outcome)
    {
        var details = _hook?.IsCurrentEventInjected == true ? " [injected]" : string.Empty;
        if (!isKeyDown && e.KeyCode == Keys.Space)
            details += $" [held {(DateTime.Now.Ticks - _spaceHoldTick) / TimeSpan.TicksPerMillisecond} ms, {_spaceRepeatCount} repeats]";

        KeyboardLog.Write($"{e.KeyCode} {(isKeyDown ? "down" : "up")}: {outcome}{details}");
    }

    // Last line of defence: the listener should never miss a key-up, but if one ever goes missing Space
    // must not stay blocked. While Space is considered held, keep checking that it really is.
    private void StartSpaceWatchdog()
    {
        if (_spaceWatchdog == null)
        {
            _spaceWatchdog = new System.Windows.Forms.Timer { Interval = SPACE_WATCHDOG_INTERVAL };
            _spaceWatchdog.Tick += (_, _) => CheckSpaceStillHeld();
        }

        _spaceWatchdog.Start();
    }

    private void CheckSpaceStillHeld()
    {
        // Windows' key state can change a moment before the matching input reaches us: only act when
        // Space has been reported as released on two consecutive checks.
        _spaceReleasedChecks = _spaceIsDown && !_hook.IsKeyHeld(Keys.Space) ? _spaceReleasedChecks + 1 : 0;

        if (_spaceReleasedChecks >= 2)
        {
            // Released while we were not looking: replay the key-up so a long hold still closes the preview.
            KeyboardLog.Write("Space: key-up was not received, replaying it");
            _spaceReleasedChecks = 0;
            CallViewWindowManagerInvokeRoutine(new KeyEventArgs(Keys.Space), false);
        }

        // Note: a pause in Space's auto-repeat does not mean it was released: the keyboard only repeats the
        // last key pressed, so pressing an arrow while holding Space stops Space's repeat for good.

        if (!_spaceIsDown)
            _spaceWatchdog.Stop();
    }

    private void InvokeRoutine(Keys key, bool isKeyDown, bool doubleTap = false)
    {
        Debug.WriteLine($"InvokeRoutine: key={key},down={isKeyDown}");

        // let the user type in an editable text field of the viewer (e.g. the text editor) without
        // Space closing the preview or Enter opening the file
        if (key != Keys.F11 && WindowHelper.IsForegroundWindowBelongToSelf() && IsTypingInTextInput())
        {
            KeyboardLog.Write($"{key} {(isKeyDown ? "down" : "up")}: ignored: typing in QuickLook");
            return;
        }

        // this Escape only left the editor of the preview: keep the preview open
        if (key == Keys.Escape && !isKeyDown && ViewWindowManager.GetInstance().ConsumeEscape())
        {
            KeyboardLog.Write("Escape up: ignored: it left the editor");
            return;
        }

        // Space would close the preview (or replace it) and lose the edits: ignore it until they are saved
        if (key == Keys.Space && ViewWindowManager.GetInstance().HasUnsavedChanges())
        {
            KeyboardLog.Write($"{key} {(isKeyDown ? "down" : "up")}: ignored: the preview has unsaved edits");
            return;
        }

        if (isKeyDown)
        {
            switch (key)
            {
                case Keys.Enter:
                    PipeServerManager.SendMessage(PipeMessages.RunAndClose);
                    break;

                case Keys.Space:
                    PipeServerManager.SendMessage(doubleTap && SettingHelper.Get("DoubleSpaceToEdit", true, "QuickLook")
                        ? PipeMessages.Edit
                        : PipeMessages.Toggle);
                    break;

                case Keys.F5:
                    PipeServerManager.SendMessage(PipeMessages.Reload);
                    break;

                case Keys.F11:
                    PipeServerManager.SendMessage(PipeMessages.Fullscreen);
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
                    // Releasing a held Space ends the peek. Close rather than toggle: if the selection changed
                    // while Space was held (arrow keys), a toggle would open the newly selected file instead.
                    if (SettingHelper.Get("AutoCloseHolding", true, "QuickLook"))
                        PipeServerManager.SendMessage(PipeMessages.Close);
                    break;
            }
        }
    }

    private static bool IsTypingInTextInput()
    {
        var element = System.Windows.Input.Keyboard.FocusedElement as System.Windows.DependencyObject;
        while (element != null)
        {
            if (element is System.Windows.Controls.Primitives.TextBoxBase textBox)
                return !textBox.IsReadOnly;

            // AvalonEdit's TextEditor lives in the plugins, so match it (or a subclass such as
            // the TextViewerPanel) by name
            for (var type = element.GetType(); type != null; type = type.BaseType)
            {
                if (type.FullName == "ICSharpCode.AvalonEdit.TextEditor")
                    return type.GetProperty("IsReadOnly")?.GetValue(element) is false;
            }

            element = element is System.Windows.Media.Visual
                ? System.Windows.Media.VisualTreeHelper.GetParent(element)
                : System.Windows.LogicalTreeHelper.GetParent(element);
        }

        return false;
    }

    private void InstallKeyHook(KeyEventHandler downHandler, KeyEventHandler upHandler)
    {
        _hook = GlobalKeyboardHook.GetInstance();

        _hook.KeyDown += downHandler;
        _hook.KeyUp += upHandler;
    }

    private void InstallForegroundWindowHook()
    {
        // When the foreground window changes (e.g. via Alt+Tab), reset the invalid-key
        // delay so the first Space press in the newly focused Explorer window works.
        // https://github.com/QL-Win/QuickLook/issues/1939
        _winEventProc = OnForegroundWindowChanged;
        _winEventHook = User32.SetWinEventHook(
            User32.EVENT_SYSTEM_FOREGROUND, User32.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _winEventProc,
            0, 0, User32.WINEVENT_OUTOFCONTEXT);
    }

    private void OnForegroundWindowChanged(nint hWinEventHook, uint eventType, nint hwnd,
        int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        // A different window is now in the foreground. Any invalid key presses that happened
        // before this switch (e.g. Alt+Tab keystrokes) belong to the previous context,
        // so they must not suppress valid keys in the new window.
        Interlocked.Exchange(ref _lastInvalidKeyPressTick, 0L);

        KeyboardLog.Write($"Foreground: {DescribeProcess(hwnd)}");

#if false // The problem of requiring two spaces has been solved -- comment the test code first.
        Debug.WriteLine($"Foreground window changed to {hwnd:X}, invalid-key delay cleared.");
#endif
    }

    // Elevated windows and other apps grabbing the focus explain many "ignored: focus" decisions.
    private static string DescribeProcess(nint hwnd)
    {
        try
        {
            User32.GetWindowThreadProcessId(hwnd, out var pid);
            using var process = Process.GetProcessById((int)pid);

            string elevation;
            try
            {
                _ = process.Handle; // needs PROCESS_ALL_ACCESS, denied for elevated processes
                elevation = "normal";
            }
            catch
            {
                elevation = "ELEVATED";
            }

            return $"{process.ProcessName} ({elevation})";
        }
        catch (Exception e)
        {
            return $"unknown ({e.GetType().Name})";
        }
    }

    internal static KeystrokeDispatcher GetInstance()
    {
        return _instance ??= new KeystrokeDispatcher();
    }
}
