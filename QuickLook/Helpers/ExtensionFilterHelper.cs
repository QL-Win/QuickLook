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

using QuickLook.Common.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuickLook.Helpers;

/// <summary>
/// Helper class for managing file extension allowlist/blocklist filtering.
/// <para>
/// <b>Blocklist mode (default):</b> All extensions are allowed except those in the blocklist.
/// If the blocklist is empty, all files are allowed.
/// </para>
/// <para>
/// <b>Allowlist mode:</b> Only extensions in the allowlist can be previewed.
/// If the allowlist is empty in allowlist mode, all files are allowed (no filtering).
/// </para>
/// <para>
/// Directories and files without extensions are always allowed regardless of the mode.
/// </para>
/// </summary>
public static class ExtensionFilterHelper
{
    private const string AllowlistKey = "ExtensionAllowlist";
    private const string BlocklistKey = "ExtensionBlocklist";
    private const string UseAllowlistModeKey = "UseExtensionAllowlistMode";
    private static readonly char[] ExtensionSeparators = [';', ','];

    private static HashSet<string> _allowlistCache;
    private static HashSet<string> _blocklistCache;
    private static bool? _useAllowlistModeCache;

    /// <summary>
    /// Gets or sets whether to use allowlist mode.
    /// When true, only extensions in the allowlist can be previewed.
    /// When false (default), extensions in the blocklist are blocked from preview.
    /// </summary>
    public static bool UseAllowlistMode
    {
        get
        {
            _useAllowlistModeCache ??= SettingHelper.Get(UseAllowlistModeKey, false);
            return _useAllowlistModeCache.Value;
        }
        set
        {
            _useAllowlistModeCache = value;
            SettingHelper.Set(UseAllowlistModeKey, value);
        }
    }

    /// <summary>
    /// Gets the current allowlist of file extensions.
    /// Extensions should be in the format ".ext" (with leading dot).
    /// </summary>
    public static HashSet<string> Allowlist
    {
        get
        {
            if (_allowlistCache == null)
            {
                var list = SettingHelper.Get(AllowlistKey, string.Empty);
                _allowlistCache = ParseExtensionList(list);
            }
            return _allowlistCache;
        }
    }

    /// <summary>
    /// Gets the current blocklist of file extensions.
    /// Extensions should be in the format ".ext" (with leading dot).
    /// </summary>
    public static HashSet<string> Blocklist
    {
        get
        {
            if (_blocklistCache == null)
            {
                var list = SettingHelper.Get(BlocklistKey, string.Empty);
                _blocklistCache = ParseExtensionList(list);
            }
            return _blocklistCache;
        }
    }

    /// <summary>
    /// Sets the allowlist of file extensions.
    /// </summary>
    /// <param name="extensions">Collection of extensions in the format ".ext" (with leading dot).</param>
    public static void SetAllowlist(IEnumerable<string> extensions)
    {
        var normalized = NormalizeExtensions(extensions);
        _allowlistCache = normalized;
        SettingHelper.Set(AllowlistKey, string.Join(";", normalized));
    }

    /// <summary>
    /// Sets the blocklist of file extensions.
    /// </summary>
    /// <param name="extensions">Collection of extensions in the format ".ext" (with leading dot).</param>
    public static void SetBlocklist(IEnumerable<string> extensions)
    {
        var normalized = NormalizeExtensions(extensions);
        _blocklistCache = normalized;
        SettingHelper.Set(BlocklistKey, string.Join(";", normalized));
    }

    /// <summary>
    /// Checks if a file path is allowed for preview based on the current filter settings.
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns>True if the file is allowed for preview, false if it should be blocked.</returns>
    public static bool IsExtensionAllowed(string path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        var extension = Path.GetExtension(path);
        
        // Files without extensions are always allowed (includes directories)
        if (string.IsNullOrEmpty(extension))
            return true;

        extension = extension.ToLowerInvariant();

        if (UseAllowlistMode)
        {
            // In allowlist mode: only allow if extension is in the allowlist
            // If allowlist is empty, allow all (no filtering)
            return Allowlist.Count == 0 || Allowlist.Contains(extension);
        }
        else
        {
            // In blocklist mode: block if extension is in the blocklist
            return !Blocklist.Contains(extension);
        }
    }

    /// <summary>
    /// Clears the cached settings, forcing a reload from the config file.
    /// </summary>
    public static void ClearCache()
    {
        _allowlistCache = null;
        _blocklistCache = null;
        _useAllowlistModeCache = null;
    }

    private static HashSet<string> ParseExtensionList(string list)
    {
        if (string.IsNullOrWhiteSpace(list))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return new HashSet<string>(
            list.Split(ExtensionSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeExtension)
                .Where(e => !string.IsNullOrEmpty(e)),
            StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> NormalizeExtensions(IEnumerable<string> extensions)
    {
        return new HashSet<string>(
            extensions.Select(NormalizeExtension).Where(e => !string.IsNullOrEmpty(e)),
            StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeExtension(string ext)
    {
        if (string.IsNullOrWhiteSpace(ext))
            return null;

        ext = ext.Trim().ToLowerInvariant();
        if (!ext.StartsWith("."))
            ext = "." + ext;

        return ext;
    }
}
