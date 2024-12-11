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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;

namespace QuickLook.NativeMethods;

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

    internal static void Init()
    {
        try
        {
            if (App.Is64Bit)
                Init_64();
            else
                Init_32();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    internal static FocusedWindowType GetFocusedWindowType()
    {
        try
        {
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
        StringBuilder sb = new StringBuilder(MaxPath);
        // communicate with COM in a separate STA thread
        var thread = new Thread(() =>
        {
            try
            {
                if (App.Is64Bit)
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
        if (sb.Length > 2 && sb[0].Equals('"') && sb[sb.Length - 1].Equals('"'))
        {
            // We got a quoted string which breaks ResolveShortcut
            sb = sb.Replace("\"", string.Empty, 0, 1).Replace("\"", string.Empty, sb.Length - 1, 1);
        }
        return ResolveShortcut(sb?.ToString() ?? String.Empty);
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
        MultiCommander
    }
}
