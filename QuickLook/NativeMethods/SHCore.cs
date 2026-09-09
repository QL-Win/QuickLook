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

namespace QuickLook.NativeMethods;

internal static class SHCore
{
    public enum PROCESS_DPI_AWARENESS
    {
        PROCESS_DPI_UNAWARE,
        PROCESS_SYSTEM_DPI_AWARE,
        PROCESS_PER_MONITOR_DPI_AWARE
    }

    [DllImport("shcore.dll")]
    public static extern uint SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

    /// <summary>
    /// DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2. Unlike the V1 context (set via
    /// SetProcessDpiAwareness), V2 makes Windows raise WM_DPICHANGED and rescale a window as it is
    /// dragged across monitors with different DPI. This keeps WPF's per-window DPI in sync, which
    /// prevents WindowChromeWorker._HandleNCHitTest from overflowing on a non-primary 4K display.
    /// </summary>
    public static readonly nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    [DllImport("user32.dll")]
    public static extern bool SetProcessDpiAwarenessContext(nint value);
}
