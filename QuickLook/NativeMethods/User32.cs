// Copyright © 2017-2025 QL-Win Contributors
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

internal static class User32
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern MessageBoxResult MessageBoxW(nint hWnd, string text, string caption, MessageBoxType type);

    [Flags]
    public enum MessageBoxType : uint
    {
        Ok = 0x00000000, // Buttons
        OkCancel = 0x00000001,
        AbortRetryIgnore = 0x00000002,
        YesNoCancel = 0x00000003,
        YesNo = 0x00000004,
        RetryCancel = 0x00000005,
        CancelTryContinue = 0x00000006,

        IconHand = 0x00000010, // Icons
        IconQuestion = 0x00000020,
        IconExclamation = 0x00000030,
        IconAsterisk = 0x00000040,
        IconWarning = IconExclamation,
        IconError = IconHand,
        IconInformation = IconAsterisk,

        DefButton1 = 0x00000000, // Default button
        DefButton2 = 0x00000100,
        DefButton3 = 0x00000200,
        DefButton4 = 0x00000300,

        ApplModal = 0x00000000, // Modality
        SystemModal = 0x00001000,
        TaskModal = 0x00002000,

        Help = 0x00004000, // Other options
        SetForeground = 0x00010000,
        TopMost = 0x00040000,
        Right = 0x00080000,
        RtlReading = 0x00100000,
    }

    public enum MessageBoxResult : int
    {
        IDOK = 1,
        IDCANCEL = 2,
        IDABORT = 3,
        IDRETRY = 4,
        IDIGNORE = 5,
        IDYES = 6,
        IDNO = 7,
        IDTRYAGAIN = 10,
        IDCONTINUE = 11
    }
}
