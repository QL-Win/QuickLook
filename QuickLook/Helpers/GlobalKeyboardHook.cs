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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using QuickLook.Common.NativeMethods;

namespace QuickLook.Helpers;

/// <summary>
/// Global keyboard listener running on its own thread; KeyDown and KeyUp are raised on that thread.
/// </summary>
/// <remarks>
/// It is built on Raw Input rather than a low-level keyboard hook (WH_KEYBOARD_LL). QuickLook only observes
/// keys and never blocks them, and a low-level hook proved unreliable for that: Windows silently removes it
/// when a callback is slow, it is not called for keys going to elevated windows (so key-ups get lost), and
/// other hooks in the chain (AutoHotkey, PowerToys, ...) delay or swallow events. Raw Input has none of these
/// problems: while this thread is busy, input waits in its queue instead of being lost.
/// </remarks>
internal partial class GlobalKeyboardHook : IDisposable
{
    private const int SLOW_HANDLER_THRESHOLD = 200;

    private static GlobalKeyboardHook _instance;

    // Keys currently held according to the input stream; only touched on the listener thread.
    private readonly HashSet<Keys> _pressedKeys = [];
    private SynchronizationContext _context;

    protected GlobalKeyboardHook()
    {
        using var ready = new ManualResetEventSlim();
        var thread = new Thread(() => RunListenerThread(ready))
        {
            IsBackground = true,
            Name = "QuickLook.GlobalKeyboardHook",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _context?.Post(_ => Application.ExitThread(), null);
        _context = null;
    }

    internal event KeyEventHandler KeyDown;

    internal event KeyEventHandler KeyUp;

    /// <summary>
    /// Whether the event being raised was synthesized by software (SendInput) rather than typed.
    /// Only meaningful inside KeyDown / KeyUp handlers.
    /// </summary>
    internal bool IsCurrentEventInjected { get; private set; }

    /// <summary>
    /// Whether the key-down being raised is an auto-repeat of a key already held.
    /// Only meaningful inside KeyDown handlers.
    /// </summary>
    internal bool IsCurrentEventRepeat { get; private set; }

    internal static GlobalKeyboardHook GetInstance()
    {
        return _instance ??= new GlobalKeyboardHook();
    }

    /// <summary>
    /// Runs the action on the listener thread and waits for it, so that state shared with the
    /// KeyDown / KeyUp handlers is only ever touched from one thread.
    /// </summary>
    internal void Invoke(Action action)
    {
        _context?.Send(_ => action(), null);
    }

    /// <summary>
    /// Whether the key is held, as tracked from the input stream and confirmed by Windows.
    /// Must be called on the listener thread.
    /// </summary>
    internal bool IsKeyHeld(Keys key)
    {
        if (!_pressedKeys.Contains(key))
            return false;

        // A key-up can still go missing, e.g. for a key held while the secure desktop (UAC, Ctrl+Alt+Del)
        // was shown. Trust Windows when it says the key is up, so a stale entry heals by itself.
        if (IsKeyDown(key))
            return true;

        _pressedKeys.Remove(key);
        return false;
    }

    // WPF's Keyboard class is bound to the UI thread, so query the system-wide key state instead.
    internal static bool IsKeyDown(Keys key)
    {
        return (User32.GetAsyncKeyState((int)key) & 0x8000) != 0;
    }

    private void RunListenerThread(ManualResetEventSlim ready)
    {
        _context = new WindowsFormsSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(_context);

        using var rawKeyboard = new RawKeyboardMonitor();
        rawKeyboard.KeyInput += OnKeyInput;
        KeyboardLog.Write("Listener: started (Raw Input)");

        ready.Set();
        Application.Run();
    }

    private void OnKeyInput(RawKeyEvent input)
    {
        var key = NormalizeModifier(input.Key);

        var isRepeat = input.IsDown && _pressedKeys.Contains(key);
        if (input.IsDown)
            _pressedKeys.Add(key);
        else
            _pressedKeys.Remove(key);

        // QuickLook itself may replay third-party hotkeys through SendInput.
        // These events are tagged in dwExtraInfo and must be forwarded without
        // entering QuickLook's hotkey pipeline, otherwise they can pollute state
        // (for example invalid-key timing windows) and cause false toggles.
        if (input.ExtraInfo == QUICKLOOK_THIRD_PARTY_HOTKEY_REPLAY_EXTRA_INFO)
            return;

        // Ignore Win+key presses, but always deliver releases: dropping a key-up would leave
        // the dispatcher believing the key is still held.
        if (input.IsDown && (IsKeyHeld(Keys.LWin) || IsKeyHeld(Keys.RWin)))
        {
            if (input.Key == Keys.Space && !isRepeat)
                KeyboardLog.Write("Listener: Space down ignored, Windows key held");
            return;
        }

        var kea = new KeyEventArgs(AddModifiers(input.Key));
        IsCurrentEventInjected = input.IsInjected;
        IsCurrentEventRepeat = isRepeat;
        var start = Environment.TickCount;

        try
        {
            if (input.IsDown)
                KeyDown?.Invoke(this, kea);
            else
                KeyUp?.Invoke(this, kea);
        }
        catch (Exception e)
        {
            KeyboardLog.Write($"Listener: handler threw {e.GetType().Name}: {e.Message}");
        }

        var elapsed = Environment.TickCount - start;
        if (elapsed > SLOW_HANDLER_THRESHOLD)
            KeyboardLog.Write($"Listener: slow handler {elapsed} ms for {kea.KeyCode} {(input.IsDown ? "down" : "up")}");
    }

    // Raw Input reports the generic VK_SHIFT / VK_CONTROL / VK_MENU; track them under one name each.
    private static Keys NormalizeModifier(Keys key)
    {
        return key switch
        {
            Keys.LShiftKey or Keys.RShiftKey => Keys.ShiftKey,
            Keys.LControlKey or Keys.RControlKey => Keys.ControlKey,
            Keys.LMenu or Keys.RMenu => Keys.Menu,
            _ => key,
        };
    }

    private Keys AddModifiers(Keys key)
    {
        // Ctrl
        if (IsKeyHeld(Keys.ControlKey))
            key |= Keys.Control;

        // Shift
        if (IsKeyHeld(Keys.ShiftKey))
            key |= Keys.Shift;

        // Alt
        if (IsKeyHeld(Keys.Menu))
            key |= Keys.Alt;

        return key;
    }
}

internal partial class GlobalKeyboardHook
{
    // Marker written to INPUT::dwExtraInfo for synthetic hotkeys replayed by QuickLook
    // through SendInput when integrating with third-party file managers.
    //
    // The keyboard listener checks this value and ignores the event,
    // so QuickLook does not treat its own replayed keystrokes as real user input.
    // This prevents false invalid-key suppression and accidental preview state changes.
    // 0x514C5452 == 'QLTR' (QuickLook Third-party Replay)
    public const int QUICKLOOK_THIRD_PARTY_HOTKEY_REPLAY_EXTRA_INFO = 0x514C5452;
}
