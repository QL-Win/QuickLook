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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QuickLook.Helpers;

/// <summary>
/// Receives every keystroke of the session through Raw Input, whichever window has the focus.
/// Unlike a low-level keyboard hook, Raw Input is not part of the hook chain: Windows never removes it for
/// being slow, other hooks cannot delay or swallow it, and while this thread is busy input waits in its queue
/// instead of being lost. It only observes; keys always reach their target unchanged.
/// Must be created on a thread with a message loop; KeyInput is raised on that thread.
/// </summary>
internal sealed class RawKeyboardMonitor : NativeWindow, IDisposable
{
    private const int WM_INPUT = 0x00FF;
    private const uint RID_INPUT = 0x10000003;
    private const uint RIM_TYPEKEYBOARD = 1;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private const uint RIDEV_REMOVE = 0x00000001;
    private const ushort RI_KEY_BREAK = 0x01;
    private const ushort VK_FAKE = 0xFF; // part of an escaped scan code sequence, not a key

    public RawKeyboardMonitor()
    {
        CreateHandle(new CreateParams());

        if (!Register(RIDEV_INPUTSINK, Handle))
            KeyboardLog.Write($"Raw: RegisterRawInputDevices failed, error {Marshal.GetLastWin32Error()}");
    }

    /// <summary>
    /// Raised for every key-down (auto-repeat included) and key-up.
    /// </summary>
    public event Action<RawKeyEvent> KeyInput;

    public void Dispose()
    {
        Register(RIDEV_REMOVE, IntPtr.Zero);
        DestroyHandle();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_INPUT)
        {
            try
            {
                OnInput(m.LParam);
            }
            catch (Exception e)
            {
                KeyboardLog.Write($"Raw: {e.GetType().Name}: {e.Message}");
            }
        }

        base.WndProc(ref m);
    }

    private void OnInput(nint hRawInput)
    {
        var size = 0u;
        var headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
        GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref size, headerSize);
        if (size == 0)
            return;

        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (GetRawInputData(hRawInput, RID_INPUT, buffer, ref size, headerSize) != size)
                return;

            var header = (RAWINPUTHEADER)Marshal.PtrToStructure(buffer, typeof(RAWINPUTHEADER));
            if (header.dwType != RIM_TYPEKEYBOARD)
                return;

            var keyboard = (RAWKEYBOARD)Marshal.PtrToStructure(buffer + (int)headerSize, typeof(RAWKEYBOARD));
            if (keyboard.VKey == 0 || keyboard.VKey == VK_FAKE)
                return;

            KeyInput?.Invoke(new RawKeyEvent(
                (Keys)keyboard.VKey,
                (keyboard.Flags & RI_KEY_BREAK) == 0,
                header.hDevice,
                keyboard.ExtraInformation));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static bool Register(uint flags, nint target)
    {
        var device = new RAWINPUTDEVICE
        {
            usUsagePage = 0x01, // generic desktop
            usUsage = 0x06, // keyboard
            dwFlags = flags,
            hwndTarget = target,
        };

        return RegisterRawInputDevices([device], 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public nint hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public nint hDevice;
        public nint wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(nint hRawInput, uint uiCommand, nint pData, ref uint pcbSize, uint cbSizeHeader);
}

internal readonly struct RawKeyEvent(Keys key, bool isDown, nint device, uint extraInfo)
{
    public Keys Key { get; } = key;

    public bool IsDown { get; } = isDown;

    /// <summary>
    /// Source keyboard; zero for input synthesized by software (SendInput).
    /// </summary>
    public nint Device { get; } = device;

    /// <summary>
    /// The dwExtraInfo passed to SendInput, if any.
    /// </summary>
    public uint ExtraInfo { get; } = extraInfo;

    public bool IsInjected => Device == IntPtr.Zero;
}
