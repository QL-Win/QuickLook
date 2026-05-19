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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using WinFormsSendKeys = System.Windows.Forms.SendKeys;
using WpfClipboard = System.Windows.Clipboard;
using WpfDataObject = System.Windows.IDataObject;

namespace QuickLook.Helpers;

internal static class FilePilotHelper
{
    private const string CopyAsPathHotkey = "^+c";
    private const int ClipboardPollIntervalMs = 5;
    private const int ClipboardTimeoutMs = 250;
    private const int MaxClassNameLength = 256;
    private const string ProcessName = "FPilot";

    internal static bool IsPreviewContext()
    {
        return IsForegroundFilePilot() && !IsTextInputFocused();
    }

    internal static bool IsForegroundFilePilot()
    {
        var hwnd = User32.GetForegroundWindow();
        return hwnd != IntPtr.Zero && IsFilePilotWindow(hwnd);
    }

    internal static string GetCurrentSelection()
    {
        if (!IsPreviewContext())
            return string.Empty;

        var hadBackup = TryGetClipboard(out var backup);

        try
        {
            ClearClipboard();
            WinFormsSendKeys.SendWait(CopyAsPathHotkey);

            return NormalizePath(WaitForClipboardText());
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return string.Empty;
        }
        finally
        {
            RestoreClipboard(backup, hadBackup);
        }
    }

    private static bool IsFilePilotWindow(nint hwnd)
    {
        try
        {
            User32.GetWindowThreadProcessId(hwnd, out var processId);
            if (processId == 0)
                return false;

            using var process = Process.GetProcessById((int)processId);
            return string.Equals(process.ProcessName, ProcessName, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return false;
        }
    }

    private static bool IsTextInputFocused()
    {
        return IsWin32TextInputFocused() || IsAutomationTextInputFocused();
    }

    private static bool IsWin32TextInputFocused()
    {
        var foregroundWindow = User32.GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
            return false;

        var foregroundThread = User32.GetWindowThreadProcessId(foregroundWindow, out _);
        var currentThread = Kernel32.GetCurrentThreadId();
        var attached = false;

        try
        {
            if (foregroundThread != currentThread)
                attached = User32.AttachThreadInput(currentThread, foregroundThread, true) != IntPtr.Zero;

            var focused = User32.GetFocus();
            if (focused == IntPtr.Zero)
                return false;

            var className = GetClassName(focused);
            return IsTextInputClass(className);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return false;
        }
        finally
        {
            if (attached)
                User32.AttachThreadInput(currentThread, foregroundThread, false);
        }
    }

    private static bool IsAutomationTextInputFocused()
    {
        try
        {
            var foregroundWindow = User32.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
                return false;

            User32.GetWindowThreadProcessId(foregroundWindow, out var foregroundProcessId);
            var focused = AutomationElement.FocusedElement;
            if (focused == null || focused.Current.ProcessId != foregroundProcessId)
                return false;

            var controlType = focused.Current.ControlType;
            if (controlType == ControlType.Edit || controlType == ControlType.ComboBox)
                return true;

            return IsTextInputClass(focused.Current.ClassName);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return false;
        }
    }

    private static bool IsTextInputClass(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
            return false;

        return className.StartsWith("Edit", StringComparison.OrdinalIgnoreCase) ||
               className.StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase) ||
               className.StartsWith("WindowsForms10.EDIT", StringComparison.OrdinalIgnoreCase) ||
               className.StartsWith("Scintilla", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetClassName(nint hwnd)
    {
        var className = new StringBuilder(MaxClassNameLength);
        User32.GetClassName(hwnd, className, className.Capacity);
        return className.ToString();
    }

    private static bool TryGetClipboard(out WpfDataObject data)
    {
        data = null;

        try
        {
            data = WpfClipboard.GetDataObject();
            return data != null;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return false;
        }
    }

    private static void ClearClipboard()
    {
        try
        {
            WpfClipboard.Clear();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    private static void RestoreClipboard(WpfDataObject data, bool hadBackup)
    {
        try
        {
            if (hadBackup && data != null)
                WpfClipboard.SetDataObject(data, true);
            else
                WpfClipboard.Clear();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    private static string WaitForClipboardText()
    {
        var start = Environment.TickCount;

        while (Environment.TickCount - start < ClipboardTimeoutMs)
        {
            try
            {
                if (WpfClipboard.ContainsText())
                    return WpfClipboard.GetText();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            Thread.Sleep(ClipboardPollIntervalMs);
        }

        return string.Empty;
    }

    private static string NormalizePath(string value)
    {
        var path = value?.Trim() ?? string.Empty;
        if (path.Length == 0)
            return string.Empty;

        if (path.Contains("\r") || path.Contains("\n"))
            path = path.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? string.Empty;

        if (path.Length >= 2 && path[0] == '"' && path[path.Length - 1] == '"')
            path = path.Substring(1, path.Length - 2);

        return path;
    }
}
