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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;

namespace QuickLook.NativeMethods;

internal readonly struct VirtualItemInfo
{
    public const string Prefix = "::QL_VIRTUAL|";
    private const long MaxFileTime = 2650467743999999999L; // DateTime.MaxValue.ToFileTime()

    public string DisplayName { get; }
    public long? FileSize { get; }
    public DateTime? DateModified { get; }
    public int IconIndex { get; }
    public string ParsingName { get; }

    public string EffectiveName => string.IsNullOrEmpty(DisplayName) ? ParsingName : DisplayName;

    public static bool IsVirtual(string path) =>
        !string.IsNullOrEmpty(path) && path.StartsWith(Prefix, StringComparison.Ordinal);

    public static bool TryParse(string path, out VirtualItemInfo info)
    {
        info = default;
        if (!IsVirtual(path))
            return false;

        var parts = path.Substring(Prefix.Length).Split(new[] { '|' }, 5);
        if (parts.Length < 5)
            return false;

        var fileSize = long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var size) && size >= 0 ? (long?)size : null;
        _ = long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var fileTime);
        var iconIndex = int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx) ? idx : -1;

        DateTime? dt = (fileTime > 0 && fileTime <= MaxFileTime)
            ? DateTime.FromFileTime(fileTime) : null;

        info = new VirtualItemInfo(parts[3], fileSize, dt, iconIndex, parts[4]);
        return true;
    }

    public static bool IsSameItem(string left, string right)
    {
        if (TryParse(left, out var a) && TryParse(right, out var b))
            return string.Equals(a.ParsingName, b.ParsingName, StringComparison.Ordinal);

        return string.Equals(left, right, StringComparison.Ordinal);
    }

    private VirtualItemInfo(string displayName, long? fileSize, DateTime? dateModified, int iconIndex, string parsingName)
    {
        DisplayName = displayName;
        FileSize = fileSize;
        DateModified = dateModified;
        IconIndex = iconIndex;
        ParsingName = parsingName;
    }
}

internal static class QuickLook
{
    private const int MaxPath = 32767;

    [DllImport("QuickLook.Native32.dll", EntryPoint = "Init",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void Init_32();

    [DllImport("QuickLook.Native32.dll", EntryPoint = "GetFocusedWindowType",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern FocusedWindowType GetFocusedWindowTypeNative_32();

    [DllImport("QuickLook.Native32.dll", EntryPoint = "GetCurrentSelection",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void GetCurrentSelectionNative_32([MarshalAs(UnmanagedType.LPWStr)] StringBuilder sb);

    [DllImport("QuickLook.Native64.dll", EntryPoint = "Init",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void Init_64();

    [DllImport("QuickLook.Native64.dll", EntryPoint = "GetFocusedWindowType",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern FocusedWindowType GetFocusedWindowTypeNative_64();

    [DllImport("QuickLook.Native64.dll", EntryPoint = "GetCurrentSelection",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void GetCurrentSelectionNative_64([MarshalAs(UnmanagedType.LPWStr)] StringBuilder sb);

    [DllImport("QuickLook.NativeArm64.dll", EntryPoint = "Init",
    CallingConvention = CallingConvention.Cdecl)]
    private static extern void Init_arm64();

    [DllImport("QuickLook.NativeArm64.dll", EntryPoint = "GetFocusedWindowType",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern FocusedWindowType GetFocusedWindowTypeNative_arm64();

    [DllImport("QuickLook.NativeArm64.dll", EntryPoint = "GetCurrentSelection",
        CallingConvention = CallingConvention.Cdecl)]
    private static extern void GetCurrentSelectionNative_arm64([MarshalAs(UnmanagedType.LPWStr)] StringBuilder sb);

    internal static void Init()
    {
        try
        {
            if (App.IsArm64)
                Init_arm64();
            else if (App.Is64Bit)
                Init_64();
            else
                Init_32();
        } catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    internal static FocusedWindowType GetFocusedWindowType()
    {
        try
        {
            if (App.IsArm64)
                return GetFocusedWindowTypeNative_arm64();
            else
                return App.Is64Bit ? GetFocusedWindowTypeNative_64() : GetFocusedWindowTypeNative_32();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return FocusedWindowType.Invalid;
        }
    }

    internal static string GetCurrentSelection()
    {
        StringBuilder sb = new(MaxPath);
        // communicate with COM in a separate STA thread
        var thread = new Thread(() =>
        {
            try
            {
                if (App.IsArm64)
                    GetCurrentSelectionNative_arm64(sb);
                else if (App.Is64Bit)
                    GetCurrentSelectionNative_64(sb);
                else
                    GetCurrentSelectionNative_32(sb);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        var raw = sb.ToString();
        if (VirtualItemInfo.IsVirtual(raw))
            return raw;

        if (raw.Length >= 2 && raw.StartsWith("\"") && raw.EndsWith("\""))
            raw = raw.Substring(1, raw.Length - 2);

        return ResolveShortcut(raw);
    }

    private static string ResolveShortcut(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;

        if (Path.GetExtension(path).ToLower() != ".lnk") return path;

        var link = new ShellLink();
        ((IPersistFile)link).Load(path, 0);
        var sb = new StringBuilder(MaxPath);
        ((IShellLinkW)link).GetPath(sb, sb.Capacity, out _, 0);

        return sb.Length == 0 ? path : sb.ToString();
    }

    internal enum FocusedWindowType
    {
        Invalid,
        Desktop,
        Explorer,
        Dialog,
        Everything,
        DOpus,
        MultiCommander,
        IDM,
        FilePilot,
        DeskBox,
    }
}
