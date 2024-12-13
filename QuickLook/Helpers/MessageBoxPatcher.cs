// Copyright © 2024 KamilDev
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
using System.Diagnostics;
using System.Windows;
using VioletaMessageBox = Wpf.Ui.Violeta.Controls.MessageBox;
using WindowsMessageBox = System.Windows.MessageBox;

namespace QuickLook.Helpers;

/// <summary>
/// Provides a helper class for patching MessageBox.Show method calls.
/// </summary>
public static class MessageBoxPatcher
{
    private static readonly Harmony Harmony = new("com.quicklook.messagebox.patch");

    /// <summary>
    /// Initializes the MessageBox patch by applying Harmony patches to the MessageBox.Show method overloads.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            Type originalType = typeof(WindowsMessageBox);

            /// <summary>
            /// Defines all MessageBox.Show method overloads to be patched:
            /// 1. Show(string messageBoxText)
            /// 2. Show(string messageBoxText, string caption)
            /// 3. Show(string messageBoxText, string caption, MessageBoxButton button)
            /// 4. Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
            /// 5. Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
            /// </summary>
            (string Name, Type[] Parameters)[] showMethods =
            [
                (nameof(WindowsMessageBox.Show), [typeof(string)]),
                (nameof(WindowsMessageBox.Show), [typeof(string), typeof(string)]),
                (nameof(WindowsMessageBox.Show), [typeof(string), typeof(string), typeof(MessageBoxButton)]),
                (nameof(WindowsMessageBox.Show), [typeof(string), typeof(string), typeof(MessageBoxButton), typeof(MessageBoxImage)]),
                (nameof(WindowsMessageBox.Show), [typeof(Window), typeof(string), typeof(string), typeof(MessageBoxButton), typeof(MessageBoxImage)])
            ];

            // Iterate over each MessageBox.Show method overload and apply the patch
            foreach (var (name, parameters) in showMethods)
            {
                var method = originalType.GetMethod(name, parameters);
                if (method != null)
                {
                    Harmony.Patch(method, new HarmonyMethod(typeof(MessageBoxPatches), nameof(MessageBoxPatches.Prefix)));
                }
            }
        }
        catch (Exception ex)
        {
            // Log any errors that occur during patch initialization
            Debug.WriteLine($"Failed to patch MessageBox: {ex}");
        }
    }
}

/// <summary>
/// Provides a patch for the MessageBox.Show method overloads.
/// </summary>
file static class MessageBoxPatches
{
    /// <summary>
    /// Prefix patch for the MessageBox.Show method overloads.
    /// </summary>
    /// <param name="__result">The result of the MessageBox.Show method call.</param>
    /// <param name="__args">The arguments passed to the MessageBox.Show method call.</param>
    /// <returns>True to skip the original method call, false to run the original method call.</returns>
    public static bool Prefix(ref MessageBoxResult __result, object[] __args)
    {
        try
        {
            // Map each Windows MessageBox.Show overload to its Violeta equivalent
            __result = __args.Length switch
            {
                // Show(string messageBoxText)
                1 => VioletaMessageBox.Show(
                    (string)__args[0]),

                // Show(string messageBoxText, string caption)
                2 => VioletaMessageBox.Show(
                    (string)__args[0],
                    (string)__args[1]),

                // Show(string messageBoxText, string caption, MessageBoxButton button)
                3 => VioletaMessageBox.Show(
                    (string)__args[0],
                    (string)__args[1],
                    (MessageBoxButton)__args[2]),

                // Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
                4 => VioletaMessageBox.Show(
                    (string)__args[0],
                    (string)__args[1],
                    (MessageBoxButton)__args[2],
                    (MessageBoxImage)__args[3]),

                // Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
                5 => VioletaMessageBox.Show(
                    (string)__args[1], // Skip Window parameter
                    (string)__args[2],
                    (MessageBoxButton)__args[3],
                    (MessageBoxImage)__args[4]),

                _ => throw new ArgumentException($"Unexpected number of arguments: {__args.Length}")
            };

            // Skip the original method call
            return false;
        }
        catch (Exception ex)
        {
            // Log any errors that occur during patch execution
            Debug.WriteLine($"Error in MessageBox patch: {ex}");
            // Run the original method call on error
            return true;
        }
    }
}
