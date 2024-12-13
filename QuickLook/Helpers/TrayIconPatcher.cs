// Copyright © 2024 ema
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

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Forms;

namespace QuickLook.Helpers;

public static class TrayIconPatcher
{
    private static readonly Harmony Harmony = new("com.quicklook.trayicon.patch");

    public static void Initialize()
    {
        var targetMethod = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.NonPublic | BindingFlags.Instance);

        if (targetMethod != null)
        {
            _ = Harmony.Patch(targetMethod, transpiler: new HarmonyMethod(typeof(TrayIconPatcher).GetMethod(nameof(ShowContextMenuTranspiler))));
        }
    }

    public static void ShowContextMenu(this NotifyIcon icon)
    {
        var targetMethod = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.NonPublic | BindingFlags.Instance);

        targetMethod?.Invoke(icon, []);
    }

    /// <summary>
    /// We need to change the NotifyIcon.ShowContextMenu to use TPM_RIGHTBUTTON instead of TPM_VERTICAL.
    /// private void ShowContextMenu()
    /// {
    ///     if (contextMenu != null || contextMenuStrip != null)
    ///     {
    ///         NativeMethods.POINT pOINT = new NativeMethods.POINT();
    ///         UnsafeNativeMethods.GetCursorPos(pOINT);
    ///         UnsafeNativeMethods.SetForegroundWindow(new HandleRef(window, window.Handle));
    ///         if (contextMenu != null)
    ///         {
    ///             contextMenu.OnPopup(EventArgs.Empty);
    ///             SafeNativeMethods.TrackPopupMenuEx(new HandleRef(contextMenu, contextMenu.Handle), 72, pOINT.x, pOINT.y, new HandleRef(window, window.Handle), null);
    ///             UnsafeNativeMethods.PostMessage(new HandleRef(window, window.Handle), 0, IntPtr.Zero, IntPtr.Zero);
    ///         }
    ///         else if (contextMenuStrip != null)
    ///         {
    ///             contextMenuStrip.ShowInTaskbar(pOINT.x, pOINT.y);
    ///         }
    ///     }
    /// }
    /// ---
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: System.Windows.Forms.ContextMenu contextMenu
    /// Opcode: brtrue.s, Operand: System.Reflection.Emit.Label
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: System.Windows.Forms.ContextMenuStrip contextMenuStrip
    /// Opcode: brfalse, Operand: System.Reflection.Emit.Label
    /// Opcode: newobj, Operand: Void.ctor()
    /// Opcode: stloc.0, Operand:
    /// Opcode: ldloc.0, Operand:
    /// Opcode: call, Operand: Boolean GetCursorPos(POINT)
    /// Opcode: pop, Operand:
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: NotifyIconNativeWindow window
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: NotifyIconNativeWindow window
    /// Opcode: callvirt, Operand: IntPtr get_Handle()
    /// Opcode: newobj, Operand: Void.ctor(System.Object, IntPtr)
    /// Opcode: call, Operand: Boolean SetForegroundWindow(System.Runtime.InteropServices.HandleRef)
    /// Opcode: pop, Operand:
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: System.Windows.Forms.ContextMenu contextMenu
    /// Opcode: brfalse.s, Operand: System.Reflection.Emit.Label
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: System.Windows.Forms.ContextMenu contextMenu
    /// Opcode: ldsfld, Operand: System.EventArgs Empty
    /// Opcode: callvirt, Operand: Void OnPopup(System.EventArgs)
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: System.Windows.Forms.ContextMenu contextMenu
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: System.Windows.Forms.ContextMenu contextMenu
    /// Opcode: callvirt, Operand: IntPtr get_Handle()
    /// Opcode: newobj, Operand: Void.ctor(System.Object, IntPtr)
    /// Opcode: ldc.i4.s, Operand: 72
    /// Opcode: ldloc.0, Operand:
    /// Opcode: ldfld, Operand: Int32 x
    /// Opcode: ldloc.0, Operand:
    /// Opcode: ldfld, Operand: Int32 y
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: NotifyIconNativeWindow window
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: NotifyIconNativeWindow window
    /// Opcode: callvirt, Operand: IntPtr get_Handle()
    /// Opcode: newobj, Operand: Void.ctor(System.Object, IntPtr)
    /// Opcode: ldnull, Operand:
    /// Opcode: call, Operand: Boolean TrackPopupMenuEx(System.Runtime.InteropServices.HandleRef, Int32, Int32, Int32, System.Runtime.InteropServices.HandleRef, TPMPARAMS)
    /// Opcode: pop, Operand:
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: NotifyIconNativeWindow window
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: NotifyIconNativeWindow window
    /// Opcode: callvirt, Operand: IntPtr get_Handle()
    /// Opcode: newobj, Operand: Void.ctor(System.Object, IntPtr)
    /// Opcode: ldc.i4.0, Operand:
    /// Opcode: ldsfld, Operand: IntPtr Zero
    /// Opcode: ldsfld, Operand: IntPtr Zero
    /// Opcode: call, Operand: Boolean PostMessage(System.Runtime.InteropServices.HandleRef, Int32, IntPtr, IntPtr)
    /// Opcode: pop, Operand:
    /// Opcode: ret, Operand:
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: System.Windows.Forms.ContextMenuStrip contextMenuStrip
    /// Opcode: brfalse.s, Operand: System.Reflection.Emit.Label
    /// Opcode: ldarg.0, Operand:
    /// Opcode: ldfld, Operand: System.Windows.Forms.ContextMenuStrip contextMenuStrip
    /// Opcode: ldloc.0, Operand:
    /// Opcode: ldfld, Operand: Int32 x
    /// Opcode: ldloc.0, Operand:
    /// Opcode: ldfld, Operand: Int32 y
    /// Opcode: callvirt, Operand: Void ShowInTaskbar(Int32, Int32)
    /// Opcode: ret, Operand:
    /// </summary>
    /// <param name="instructions"></param>
    /// <returns></returns>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> ShowContextMenuTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        try
        {
            var codes = instructions.ToArray();

            if (Debugger.IsAttached)
            {
                // Test code to print the IL codes
                foreach (var code in codes)
                {
                    Debug.WriteLine($"Opcode: {code.opcode}, Operand: {code.operand}");
                }
            }

            // How to change source code for proxy:
            // from SafeNativeMethods.TrackPopupMenuEx(new HandleRef(contextMenu, contextMenu.Handle), 72, pOINT.x, pOINT.y, new HandleRef(window, window.Handle), null);
            // to   SafeNativeMethods.TrackPopupMenuEx(new HandleRef(contextMenu, contextMenu.Handle), 66, pOINT.x, pOINT.y, new HandleRef(window, window.Handle), null);
            // [TrackPopupMenuEx] uFlags: A set of flags that determine how the menu behaves.
            // The number 72 in binary is 0100 1000. Breaking it down:
            //  * TPM_VERTICAL(0x0040)
            //  * TPM_RIGHTALIGN(0x0008)
            // The number 64 in binary is 0100 0000. Breaking it down:
            //  * TPM_VERTICAL(0x0040)
            //  * TPM_LEFTALIGN(0x0000)
            const sbyte sourceFlag = (sbyte)(TrackPopupMenuFlags.TPM_VERTICAL | TrackPopupMenuFlags.TPM_RIGHTALIGN);
            const sbyte targetFlag = (sbyte)(TrackPopupMenuFlags.TPM_VERTICAL | TrackPopupMenuFlags.TPM_LEFTALIGN);

            for (int i = 0; i < codes.Length; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i].operand == sourceFlag)
                {
                    codes[i].operand = targetFlag;
                    break;
                }
            }

            return codes;
        }
        catch
        {
            // No fallback needed in this case
        }

        return instructions;
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-trackpopupmenuex
    /// </summary>
    [Flags]
    private enum TrackPopupMenuFlags : uint
    {
        /// <summary>
        /// The user can select menu items with only the left mouse button.
        /// </summary>
        TPM_LEFTBUTTON = 0x0000,

        /// <summary>
        /// The user can select menu items with both the left and right mouse buttons.
        /// </summary>
        TPM_RIGHTBUTTON = 0x0002,

        /// <summary>
        /// Positions the shortcut menu so that its left side is aligned with the coordinate specified by the x parameter.
        /// </summary>
        TPM_LEFTALIGN = 0x0000,

        /// <summary>
        /// Centers the shortcut menu horizontally relative to the coordinate specified by the x parameter.
        /// </summary>
        TPM_CENTERALIGN = 0x0004,

        /// <summary>
        /// Positions the shortcut menu so that its right side is aligned with the coordinate specified by the x parameter.
        /// </summary>
        TPM_RIGHTALIGN = 0x0008,

        /// <summary>
        /// Positions the shortcut menu so that its top side is aligned with the coordinate specified by the y parameter.
        /// </summary>
        TPM_TOPALIGN = 0x0000,

        /// <summary>
        /// Centers the shortcut menu vertically relative to the coordinate specified by the y parameter.
        /// </summary>
        TPM_VCENTERALIGN = 0x0010,

        /// <summary>
        /// Positions the shortcut menu so that its bottom side is aligned with the coordinate specified by the y parameter.
        /// </summary>
        TPM_BOTTOMALIGN = 0x0020,

        /// <summary>
        /// TPM_HORIZONTAL
        /// </summary>
        TPM_HORIZONTAL = 0x0000,

        /// <summary>
        /// TPM_VERTICAL
        /// </summary>
        TPM_VERTICAL = 0x0040,

        /// <summary>
        /// The function does not send notification messages when the user clicks a menu item.
        /// </summary>
        TPM_NONOTIFY = 0x0080,

        /// <summary>
        /// The function returns the menu item identifier of the user's selection in the return value.
        /// </summary>
        TPM_RETURNCMD = 0x0100,

        /// <summary>
        /// TPM_RECURSE
        /// </summary>
        TPM_RECURSE = 0x0001,

        /// <summary>
        /// Animates the menu from left to right.
        /// </summary>
        TPM_HORPOSANIMATION = 0x0400,

        /// <summary>
        /// Animates the menu from right to left.
        /// </summary>
        TPM_HORNEGANIMATION = 0x0800,

        /// <summary>
        /// Animates the menu from top to bottom.
        /// </summary>
        TPM_VERPOSANIMATION = 0x1000,

        /// <summary>
        /// Animates the menu from bottom to top.
        /// </summary>
        TPM_VERNEGANIMATION = 0x2000,

        /// <summary>
        /// Displays menu without animation.
        /// </summary>
        TPM_NOANIMATION = 0x4000,

        /// <summary>
        /// TPM_LAYOUTRTL
        /// </summary>
        TPM_LAYOUTRTL = 0x8000,

        /// <summary>
        /// TPM_WORKAREA
        /// </summary>
        TPM_WORKAREA = 0x10000,
    }
}
