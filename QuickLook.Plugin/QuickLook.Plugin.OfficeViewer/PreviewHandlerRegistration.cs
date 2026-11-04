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

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace QuickLook.Plugin.OfficeViewer;

/// <summary>
/// Helps QuickLook use preview handlers registered by 32-bit Office suites (such as WPS Office).
/// </summary>
/// <remarks>
/// WPS Office is a 32-bit application and registers its preview handlers only in the 32-bit
/// registry view (<c>HKEY_CLASSES_ROOT\WOW6432Node\CLSID</c>). QuickLook is a 64-bit process,
/// so it can neither read the display name nor activate the COM class of those handlers, and
/// the Office documents they are associated with appear to be unsupported.
///
/// For the current user only, this class mirrors such a registration into the 64-bit view
/// (<c>HKEY_CURRENT_USER\Software\Classes\CLSID</c>). This requires no elevation, does not
/// touch the machine-wide registration written by the Office suite, and is idempotent.
/// Removing the created keys restores the previous state.
///
/// Only out-of-process preview handlers (<c>LocalServer32</c>) are mirrored. A 32-bit
/// in-process handler (<c>InprocServer32</c>) cannot be loaded by a 64-bit process, so such a
/// registration is intentionally left untouched and the handler remains unavailable to QuickLook.
/// </remarks>
internal static class PreviewHandlerRegistration
{
    private const string ClassesRootPath = @"CLSID";
    private const string LocalServer32Key = @"LocalServer32";
    private const string InprocServer32Key = @"InprocServer32";
    private const string InprocHandler32Key = @"InprocHandler32";
    private const string MirrorMarkerValue = "QuickLookMirror";
    private const string Ole32 = "ole32.dll";

    /// <summary>
    /// Determines whether the specified preview handler is registered and can be activated in the
    /// registry view of the current process.
    /// </summary>
    /// <param name="clsid">The CLSID of the preview handler.</param>
    /// <returns>
    /// <see langword="true"/> when the registration is complete (a display name plus either a
    /// <c>LocalServer32</c> or an <c>InprocServer32</c> activation); otherwise
    /// <see langword="false"/>.
    /// </returns>
    public static bool IsRegisteredForCurrentView(Guid clsid)
    {
        var registration = ReadRegistration(clsid, RegistryView.Registry64);
        return registration != null && registration.IsComplete;
    }

    /// <summary>
    /// Ensures that the specified preview handler is registered in the registry view of the
    /// current process. If it is only registered in the 32-bit registry view, a mirror of that
    /// registration is created under <c>HKEY_CURRENT_USER\Software\Classes\CLSID</c>. A mirror
    /// created by QuickLook is refreshed when the 32-bit source registration changes, for example
    /// after an Office suite update moved its executable.
    /// </summary>
    /// <param name="clsid">The CLSID of the preview handler.</param>
    /// <returns>
    /// <see langword="true"/> when the handler is usable by this process after the call;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public static bool EnsureRegisteredForCurrentView(Guid clsid)
    {
        var source = ReadRegistration(clsid, RegistryView.Registry32);
        if (source == null)
        {
            // A handler that was uninstalled leaves a stale mirror behind. Remove it, but only
            // when the 32-bit key is confirmed absent: a failed read is not proof of absence.
            if (IsAbsentFrom32BitView(clsid))
                RemoveMirror(clsid);

            return false;
        }

        if (!source.IsLocalServer)
        {
            // A 32-bit in-process handler cannot be hosted by a 64-bit process, and pointing a
            // 64-bit InprocServer32 entry at a 32-bit DLL would only break activation.
            if (source.HasInprocServer)
                Debug.WriteLine($"QuickLook: preview handler {clsid:B} is registered as a 32-bit in-process server and cannot be mirrored.");
            return false;
        }

        var current = ReadRegistration(clsid, RegistryView.Registry64);
        var isMirror = IsMirror(clsid);

        // Never overwrite a registration that QuickLook did not create.
        if (current != null && !isMirror)
            return current.IsComplete;

        if (current != null && current.IsComplete && current.Matches(source))
            return true;

        return WriteMirror(clsid, source, isMirror);
    }

    /// <summary>
    /// Reads a preview handler registration from the given registry view.
    /// </summary>
    /// <param name="clsid">The CLSID of the preview handler.</param>
    /// <param name="view">The registry view to read from.</param>
    /// <returns>The registration, or <see langword="null"/> when the CLSID is not registered in that view.</returns>
    private static HandlerRegistration ReadRegistration(Guid clsid, RegistryView view)
    {
        try
        {
            using var classes = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, view);
            using var clsidKey = classes.OpenSubKey($@"{ClassesRootPath}\{clsid:B}");
            if (clsidKey == null)
                return null;

            var registration = new HandlerRegistration
            {
                DisplayName = clsidKey.GetValue(string.Empty) as string,
            };

            using (var inprocHandler32 = clsidKey.OpenSubKey(InprocHandler32Key))
                registration.InprocHandler32 = inprocHandler32?.GetValue(string.Empty) as string;

            using (var inprocServer32 = clsidKey.OpenSubKey(InprocServer32Key))
                registration.InprocServer32 = inprocServer32?.GetValue(string.Empty) as string;

            using var localServer32 = clsidKey.OpenSubKey(LocalServer32Key);
            if (localServer32 != null)
            {
                // Copy every string value, including named values such as ServerExecutable, instead
                // of only the default command line. Non-string values are skipped: they cannot be
                // written back as a command line and would abort the whole mirror.
                foreach (var name in localServer32.GetValueNames())
                {
                    if (localServer32.GetValue(name) is string value)
                        registration.LocalServerValues[name] = value;
                }
            }

            return registration;
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error reading preview handler registration: " + e);
            return null;
        }
    }

    /// <summary>
    /// Determines whether the current-user 64-bit registration was created by QuickLook.
    /// </summary>
    /// <param name="clsid">The CLSID of the preview handler.</param>
    /// <returns><see langword="true"/> when the mirror marker is present.</returns>
    private static bool IsMirror(Guid clsid)
    {
        try
        {
            using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using var clsidKey = currentUser.OpenSubKey($@"Software\Classes\{ClassesRootPath}\{clsid:B}");
            return clsidKey?.GetValue(MirrorMarkerValue) != null;
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error reading preview handler mirror marker: " + e);
            return false;
        }
    }

    /// <summary>
    /// Determines whether the CLSID key is confirmed to be missing from the 32-bit registry view.
    /// </summary>
    /// <param name="clsid">The CLSID of the preview handler.</param>
    /// <returns><see langword="true"/> only when the key was successfully opened and does not exist.</returns>
    private static bool IsAbsentFrom32BitView(Guid clsid)
    {
        try
        {
            using var classes = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry32);
            using var clsidKey = classes.OpenSubKey($@"{ClassesRootPath}\{clsid:B}");
            return clsidKey == null;
        }
        catch (Exception e)
        {
            // Treat an inspection error as "unknown", never as proof that the source is gone.
            Debug.WriteLine("Error checking preview handler registration: " + e);
            return false;
        }
    }

    /// <summary>
    /// Removes the current-user 64-bit registration, but only when QuickLook created it.
    /// </summary>
    /// <param name="clsid">The CLSID of the preview handler.</param>
    private static void RemoveMirror(Guid clsid)
    {
        if (!IsMirror(clsid))
            return;

        try
        {
            using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            currentUser.DeleteSubKeyTree($@"Software\Classes\{ClassesRootPath}\{clsid:B}", false);
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error removing stale preview handler mirror: " + e);
        }
    }

    /// <summary>
    /// Writes the per-user 64-bit mirror of a 32-bit out-of-process preview handler registration.
    /// </summary>
    /// <param name="clsid">The CLSID of the preview handler.</param>
    /// <param name="source">The 32-bit registration to mirror.</param>
    /// <param name="replaceExisting">Whether an existing mirror created by QuickLook may be replaced.</param>
    /// <returns><see langword="true"/> when the mirrored registration is complete afterwards.</returns>
    private static bool WriteMirror(Guid clsid, HandlerRegistration source, bool replaceExisting)
    {
        // Without a display name the mirror would never be considered registered, and writing a
        // null default value would fail halfway through. Refuse instead of leaving a partial key.
        if (source == null || string.IsNullOrWhiteSpace(source.DisplayName))
            return false;

        var keyPath = $@"Software\Classes\{ClassesRootPath}\{clsid:B}";
        var createdNew = false;

        try
        {
            using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using (var existing = currentUser.OpenSubKey(keyPath))
                createdNew = existing == null;

            using var clsid64 = currentUser.CreateSubKey(keyPath);
            if (clsid64 == null)
                return false;

            if (replaceExisting)
            {
                // Mark the mirror incomplete until the refreshed values are written, so an
                // interrupted refresh is repaired instead of being treated as registered.
                clsid64.SetValue(string.Empty, string.Empty);

                // Drop stale values of a previous mirror before copying the current source.
                clsid64.DeleteSubKeyTree(LocalServer32Key, false);
                clsid64.DeleteSubKeyTree(InprocHandler32Key, false);
            }

            using (var localServer64 = clsid64.CreateSubKey(LocalServer32Key))
            {
                if (localServer64 == null)
                    return false;

                foreach (var pair in source.LocalServerValues)
                    localServer64.SetValue(pair.Key, pair.Value);
            }

            using (var inprocHandler64 = clsid64.CreateSubKey(InprocHandler32Key))
                inprocHandler64?.SetValue(string.Empty, source.InprocHandler32 ?? Ole32);

            clsid64.SetValue(MirrorMarkerValue, "1");

            // Publish the display name last: it marks the mirror as complete, so an interrupted
            // write is repaired on the next attempt instead of being treated as registered.
            clsid64.SetValue(string.Empty, source.DisplayName);

            Debug.WriteLine($"QuickLook: mirrored preview handler {clsid:B} ('{source.DisplayName}') into the 64-bit registry view.");
            return IsRegisteredForCurrentView(clsid);
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error mirroring preview handler registration: " + e);

            // Only clean up a key that this attempt created; never delete a pre-existing one.
            if (createdNew)
            {
                try
                {
                    using var currentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
                    currentUser.DeleteSubKeyTree(keyPath, false);
                }
                catch (Exception cleanupError)
                {
                    Debug.WriteLine("Error cleaning up partial preview handler mirror: " + cleanupError);
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Describes a preview handler registration read from one registry view.
    /// </summary>
    private sealed class HandlerRegistration
    {
        /// <summary>Gets or sets the display name (the CLSID key's default value).</summary>
        public string DisplayName { get; set; }

        /// <summary>Gets or sets the <c>InprocHandler32</c> value.</summary>
        public string InprocHandler32 { get; set; }

        /// <summary>Gets or sets the <c>InprocServer32</c> value.</summary>
        public string InprocServer32 { get; set; }

        /// <summary>Gets the <c>LocalServer32</c> values, including named values.</summary>
        public Dictionary<string, string> LocalServerValues { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets a value indicating whether an executable for an out-of-process server is registered,
        /// either as the default command line or through the named <c>ServerExecutable</c> value.
        /// </summary>
        public bool IsLocalServer =>
            (LocalServerValues.TryGetValue(string.Empty, out var command) && !string.IsNullOrWhiteSpace(command))
            || (LocalServerValues.TryGetValue("ServerExecutable", out var executable) && !string.IsNullOrWhiteSpace(executable));

        /// <summary>Gets a value indicating whether an in-process server is registered.</summary>
        public bool HasInprocServer => !string.IsNullOrWhiteSpace(InprocServer32);

        /// <summary>Gets a value indicating whether the registration can be activated at all.</summary>
        public bool HasActivation => IsLocalServer || HasInprocServer;

        /// <summary>Gets a value indicating whether the registration has both a name and an activation.</summary>
        public bool IsComplete => !string.IsNullOrWhiteSpace(DisplayName) && HasActivation;

        /// <summary>
        /// Determines whether this registration has the same activation data as another one.
        /// </summary>
        /// <param name="other">The registration to compare with.</param>
        /// <returns><see langword="true"/> when the values match.</returns>
        public bool Matches(HandlerRegistration other)
        {
            if (other == null)
                return false;

            if (!string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal))
                return false;

            if (!string.Equals(InprocHandler32 ?? Ole32, other.InprocHandler32 ?? Ole32, StringComparison.OrdinalIgnoreCase))
                return false;

            if (LocalServerValues.Count != other.LocalServerValues.Count)
                return false;

            foreach (var pair in LocalServerValues)
            {
                if (!other.LocalServerValues.TryGetValue(pair.Key, out var otherValue)
                    || !string.Equals(pair.Value, otherValue, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }
    }
}
