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

using QuickLook.Common.NativeMethods;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace QuickLook.Helpers;

/// <summary>
/// Keeps Explorer's tooltips (file info tips) from covering the preview.
/// </summary>
/// <remarks>
/// Explorer's tooltips are topmost windows, so an info tip scheduled for the file under the mouse shows up on
/// top of the preview that opened in the meantime. Whenever such a tooltip overlaps a visible QuickLook window,
/// it is dismissed; everywhere else Explorer's tooltips are left alone. Runs on its own thread so it reacts
/// immediately even while the UI thread is busy loading a preview.
/// </remarks>
internal sealed class ExplorerTooltipSuppressor : IDisposable
{
    private const uint EVENT_OBJECT_SHOW = 0x8002;
    private const int OBJID_WINDOW = 0;
    private const int CHILDID_SELF = 0;
    private const uint TTM_POP = 0x041C;
    private const string TOOLTIP_CLASS = "tooltips_class32";

    private static readonly uint CurrentProcessId = (uint)Process.GetCurrentProcess().Id;

    private SynchronizationContext _context;

    public ExplorerTooltipSuppressor()
    {
        using var ready = new ManualResetEventSlim();
        var thread = new Thread(() => Run(ready))
        {
            IsBackground = true,
            Name = "QuickLook.ExplorerTooltipSuppressor",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();
    }

    public void Dispose()
    {
        _context?.Post(_ => Application.ExitThread(), null);
        _context = null;
    }

    private void Run(ManualResetEventSlim ready)
    {
        _context = new WindowsFormsSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(_context);

        User32.WinEventProc proc = OnWindowShown; // keep a reference so it is not collected
        var hook = User32.SetWinEventHook(EVENT_OBJECT_SHOW, EVENT_OBJECT_SHOW, IntPtr.Zero, proc,
            0, 0, User32.WINEVENT_OUTOFCONTEXT);

        ready.Set();
        Application.Run();

        User32.UnhookWinEvent(hook);
        GC.KeepAlive(proc);
    }

    private void OnWindowShown(nint hWinEventHook, uint eventType, nint hwnd,
        int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        if (idObject != OBJID_WINDOW || idChild != CHILDID_SELF || hwnd == IntPtr.Zero)
            return;

        try
        {
            GetWindowThreadProcessId(hwnd, out var pid);

            if (pid == CurrentProcessId)
            {
                // a preview opened: dismiss the tooltips already showing underneath it
                if (!IsTooltip(hwnd))
                    DismissOverlappingTooltips();
            }
            else if (IsTooltip(hwnd) && IsExplorer(pid) && OverlapsPreview(hwnd))
            {
                Dismiss(hwnd);
            }
        }
        catch (Exception e)
        {
            KeyboardLog.Write($"TooltipSuppressor: {e.GetType().Name}: {e.Message}");
        }
    }

    private static void DismissOverlappingTooltips()
    {
        var tooltips = new List<nint>();
        EnumWindows((hwnd, _) =>
        {
            if (IsWindowVisible(hwnd) && IsTooltip(hwnd))
                tooltips.Add(hwnd);
            return true;
        }, IntPtr.Zero);

        foreach (var tooltip in tooltips)
        {
            GetWindowThreadProcessId(tooltip, out var pid);
            if (pid != CurrentProcessId && IsExplorer(pid) && OverlapsPreview(tooltip))
                Dismiss(tooltip);
        }
    }

    private static void Dismiss(nint tooltip)
    {
        PostMessage(tooltip, TTM_POP, IntPtr.Zero, IntPtr.Zero);
    }

    private static bool OverlapsPreview(nint tooltip)
    {
        if (!GetWindowRect(tooltip, out var tip))
            return false;

        var overlaps = false;
        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == CurrentProcessId && IsWindowVisible(hwnd) && !IsTooltip(hwnd) &&
                GetWindowRect(hwnd, out var window) && Intersects(tip, window))
            {
                overlaps = true;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        return overlaps;
    }

    private static bool Intersects(RECT a, RECT b)
    {
        return a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;
    }

    private static bool IsTooltip(nint hwnd)
    {
        var className = new StringBuilder(64);
        GetClassName(hwnd, className, className.Capacity);
        return className.ToString() == TOOLTIP_CLASS;
    }

    private static bool IsExplorer(uint pid)
    {
        try
        {
            using var process = Process.GetProcessById((int)pid);
            return string.Equals(process.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false; // exited meanwhile
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private delegate bool EnumWindowsProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint hwnd, StringBuilder className, int maxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hwnd, out RECT rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint hwnd, uint msg, nint wParam, nint lParam);
}
