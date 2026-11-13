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

using QuickLook.Plugin.InfoPanel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace QuickLook.Helpers;

/// <summary>
///     Best-effort support for shell-only items, i.e. items that Explorer shows but that are not
///     accessible through the Win32 file system: files on an MTP device (a phone connected by USB),
///     items inside a zip folder, items of a virtual shell folder, ...
/// </summary>
/// <remarks>
///     For such items <see cref="File.Exists" /> and <see cref="Directory.Exists" /> return false and
///     System.IO throws <see cref="ArgumentException" /> ("Illegal characters in path") because the
///     shell parsing name contains ':' and '?' characters. The only way to get the real content is to
///     let the shell copy the item into a temporary folder through IFileOperation.
///     Every method here is defensive: it never throws, failures simply return false/null.
/// </remarks>
public static class ShellItemHelper
{
    /// <summary>Items larger than this are not copied to a temporary file.</summary>
    public const long MaxMaterializedSize = 512L * 1024 * 1024;

    private const string TempRootFolderName = "QuickLook.ShellItems";

    private static readonly string TempRoot = Path.Combine(Path.GetTempPath(), TempRootFolderName);
    private static readonly object SyncRoot = new object();
    private static readonly HashSet<string> ActiveTempFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static bool _staleCleanupDone;

    // Metadata of a shell item is requested several times per preview (routing, placeholder title,
    // info panel); walking an MTP namespace for every request is slow. This short lived cache keeps the
    // UI thread responsive by reusing the result of the background resolution.
    private static readonly Dictionary<string, CachedShellMetadata> MetadataCache =
        new Dictionary<string, CachedShellMetadata>(StringComparer.OrdinalIgnoreCase);

    private static readonly object MetadataCacheLock = new object();
    private static readonly TimeSpan MetadataCacheLifetime = TimeSpan.FromSeconds(5);

    private static readonly HashSet<string> ReservedFileNames = new HashSet<string>(
        new[]
        {
            "CON", "PRN", "AUX", "NUL", "CLOCK$",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        }, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     True when <paramref name="path" /> is not a Win32 file system path but the shell can still
    ///     resolve it to an item (MTP device item, item inside a zip folder, ...).
    /// </summary>
    public static bool IsShellOnly(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            if (File.Exists(path) || Directory.Exists(path))
                return false;

            if (ShellItemInfo.IsShellItem(path))
                return true;

            // Items of an MTP/portable device are addressed with "::{...}" descriptions that
            // SHCreateItemFromParsingName cannot always resolve on its own; the shell still knows them
            // through the Explorer selection (see TryMaterializeViaExplorerSelection).
            return path.StartsWith("::", StringComparison.Ordinal);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    ///     Copies a shell-only file item into a unique temporary folder and returns the local file path,
    ///     ready to be previewed by a normal file based plugin. Returns null when the item cannot (or
    ///     should not) be materialized: non shell paths, folders, CLSID roots handled by CLSIDViewer,
    ///     items larger than <see cref="MaxMaterializedSize" /> and any shell failure.
    /// </summary>
    public static string TryMaterialize(string path)
    {
        return TryMaterialize(path, CancellationToken.None, null);
    }

    /// <summary>
    ///     Cancellable, progress-aware version of <see cref="TryMaterialize(string)" />.
    /// </summary>
    /// <param name="path">Shell parsing name of the item.</param>
    /// <param name="cancellationToken">Aborts the wait for the copy and discards the temporary file.</param>
    /// <param name="progress">
    ///     Receives the number of copied bytes and the expected size (0 when unknown) while a shell copy
    ///     is running. Called from the worker thread.
    /// </param>
    /// <remarks>
    ///     The copy itself is performed synchronously on the calling thread, therefore callers must not
    ///     call this method from the UI thread.
    /// </remarks>
    public static string TryMaterialize(string path, CancellationToken cancellationToken,
        Action<long, long> progress)
    {
        try
        {
            if (!IsShellOnly(path) || IsExactClsidPath(path))
                return null;

            // Policy checks before choosing a copy strategy. Returning null here means "do not copy this
            // item at all", so the caller must not try another strategy: a folder or an oversized item is
            // a deliberate rejection, not a resolution failure.
            var hasMetadata = TryGetShellItemMetadata(path,
                out _, out var isFolder, out var knownSize, out _, out _);

            // Prefer the exact byte size from the property system (PKEY_Size). The Explorer column used by
            // TryGetShellItemMetadata is a rounded, localized display value; only the exact value can be
            // used to detect a completed copy.
            var exactSizeKnown = ShellItemInfo.TryGetSize(path, out var exactSize) && exactSize > 0;

            if (exactSizeKnown)
                knownSize = exactSize;

            if (hasMetadata && isFolder)
                return null;

            if (knownSize > MaxMaterializedSize)
                return null;

            if (cancellationToken.IsCancellationRequested)
                return null;

            // 1) Preferred: ask the shell item directly to copy itself (works for items inside zip folders and
            //    for shell namespaces that SHCreateItemFromParsingName can resolve).
            var materialized = TryMaterializeViaShellItem(path, cancellationToken, progress);

            if (!string.IsNullOrEmpty(materialized))
                return materialized;

            if (cancellationToken.IsCancellationRequested)
                return null;

            // 2) Portable devices (MTP phones) cannot be resolved from their parsing name on every Windows
            //    build, but the shell namespace below "This PC" knows them. Walk the namespace along the
            //    path segments, then copy the matching item. This is the main path for phone files.
            materialized = TryMaterializeViaShellNamespace(path, knownSize, exactSizeKnown, cancellationToken, progress);

            if (!string.IsNullOrEmpty(materialized))
                return materialized;

            if (cancellationToken.IsCancellationRequested)
                return null;

            // 3) Last fallback: the item is the current Explorer selection; ask the shell to copy it.
            return TryMaterializeViaExplorerSelection(path, knownSize, exactSizeKnown, cancellationToken, progress);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    ///     Resolves an MTP/portable-device item without relying on SHCreateItemFromParsingName: the item is
    ///     located by walking the "This PC" shell namespace along the segments of its absolute parsing name,
    ///     then copied to a temporary file. Returns null when the walk or the copy fails.
    /// </summary>
    private static string TryMaterializeViaShellNamespace(string path, long expectedSize,
        bool expectedSizeIsExact, CancellationToken cancellationToken, Action<long, long> progress)
    {
        var tempDirectory = null as string;

        try
        {
            dynamic targetItem;
            dynamic parentFolder;

            if (!TryWalkToItem(path, out targetItem, out parentFolder))
                return null;

            try
            {
                // A folder must never be copied to a temporary place: the whole content would be
                // transferred just to show a preview. Folders are handled by the info panel instead.
                if ((bool)targetItem.IsFolder)
                    return null;

                // Enforce the size limit inside every copy strategy, including items whose metadata was
                // not available before this namespace walk.
                if (expectedSize <= 0)
                {
                    expectedSize = ParseHumanSize((string)GetDetailsString(parentFolder, targetItem, 2));
                    expectedSizeIsExact = false;
                }

                if (expectedSize > MaxMaterializedSize)
                    return null;

                if (cancellationToken.IsCancellationRequested)
                    return null;

                var safeName = MakeSafeFileName((string)targetItem.Name);

                if (string.IsNullOrEmpty(safeName))
                    safeName = "shellitem";

                var shellType = Type.GetTypeFromProgID("Shell.Application");

                if (shellType == null)
                    return null;

                dynamic shell = Activator.CreateInstance(shellType);

                Directory.CreateDirectory(TempRoot);

                tempDirectory = Path.Combine(TempRoot, Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDirectory);

                dynamic destination = shell.NameSpace(tempDirectory);

                if (destination == null)
                {
                    TryDeleteDirectory(tempDirectory);
                    return null;
                }

                destination.CopyHere(targetItem, 0x14); // FOF_SILENT | FOF_NOCONFIRMATION

                string destinationFile;

                if (!WaitForCopyToFinish(tempDirectory, expectedSize, expectedSizeIsExact, cancellationToken,
                        progress, out destinationFile))
                {
                    // The shell copy may still be writing this folder: delete it once the transfer stops
                    // instead of blocking the worker until the device is idle.
                    ScheduleTempDirectoryCleanup(tempDirectory);
                    return null;
                }

                lock (SyncRoot)
                    ActiveTempFiles.Add(destinationFile);

                return destinationFile;
            }
            finally
            {
                ReleaseComObject(targetItem);
                ReleaseComObject(parentFolder);
            }
        }
        catch (Exception)
        {
            if (!string.IsNullOrEmpty(tempDirectory))
                ScheduleTempDirectoryCleanup(tempDirectory);

            return null;
        }
    }

    /// <summary>
    ///     Walks the "This PC" shell namespace along the segments of an absolute parsing name and returns
    ///     the matching item together with the Folder object that contains it. This is how items of an
    ///     MTP/portable device are located without SHCreateItemFromParsingName. The caller owns the
    ///     returned COM objects.
    /// </summary>
    private static bool TryWalkToItem(string path, out dynamic item, out dynamic parentFolder)
    {
        item = null;
        parentFolder = null;

        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");

            if (shellType == null)
                return false;

            dynamic shell = Activator.CreateInstance(shellType);
            dynamic thisPc = shell.NameSpace(17); // ssfDRIVES (This PC)

            if (thisPc == null)
                return false;

            dynamic deviceItem = FindPrefixItem(thisPc, path);

            if (deviceItem == null || !(bool)deviceItem.IsFolder)
                return false;

            dynamic currentFolder = deviceItem.GetFolder;
            string currentPath = deviceItem.Path;

            if (currentFolder == null)
                return false;

            var remaining = path.Substring(currentPath.Length).TrimStart('\\');
            var segments = remaining.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
                return false;

            for (var i = 0; i < segments.Length; i++)
            {
                string wantedPath = currentPath.TrimEnd('\\') + "\\" + segments[i];
                dynamic found = FindExactItem(currentFolder, wantedPath);

                if (found == null)
                    return false;

                if (i == segments.Length - 1)
                {
                    item = found;
                    parentFolder = currentFolder;
                    return true;
                }

                currentFolder = found.GetFolder;
                currentPath = wantedPath;

                if (currentFolder == null)
                    return false;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        return false;
    }

    /// <summary>
    ///     Reads the basic shell metadata of a shell-only item (MTP/portable device item, ...): real
    ///     display name, whether it is a folder, size in bytes (files only, -1 when unknown), last
    ///     modified date and the type description. Returns false when the item cannot be located.
    /// </summary>
    public static bool TryGetShellItemMetadata(string path,
        out string name, out bool isFolder, out long size, out DateTime? modified, out string typeText)
    {
        name = null;
        isFolder = false;
        size = -1;
        modified = null;
        typeText = null;

        if (TryGetCachedMetadata(path, out name, out isFolder, out size, out modified, out typeText))
            return true;

        dynamic item;
        dynamic parentFolder;

        try
        {
            if (!TryWalkToItem(path, out item, out parentFolder))
                return false;

            try
            {
                name = (string)item.Name;
                isFolder = (bool)item.IsFolder;
                typeText = (string)GetDetailsString(parentFolder, item, 1);

                var sizeText = (string)GetDetailsString(parentFolder, item, 2);
                size = ParseHumanSize(sizeText);

                var modifiedText = (string)GetDetailsString(parentFolder, item, 3);

                if (!string.IsNullOrEmpty(modifiedText)
                 && DateTime.TryParse(modifiedText, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed))
                {
                    modified = parsed;
                }

                CacheMetadata(path, name, isFolder, size, modified, typeText);
                return !string.IsNullOrEmpty(name);
            }
            finally
            {
                ReleaseComObject(item);
                ReleaseComObject(parentFolder);
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    private sealed class CachedShellMetadata
    {
        public string Name;
        public bool IsFolder;
        public long Size;
        public DateTime? Modified;
        public string TypeText;
        public DateTime CreatedUtc;
    }

    private static bool TryGetCachedMetadata(string path, out string name, out bool isFolder,
        out long size, out DateTime? modified, out string typeText)
    {
        name = null;
        isFolder = false;
        size = -1;
        modified = null;
        typeText = null;

        if (string.IsNullOrEmpty(path))
            return false;

        lock (MetadataCacheLock)
        {
            if (!MetadataCache.TryGetValue(path, out var cached))
                return false;

            if (DateTime.UtcNow - cached.CreatedUtc > MetadataCacheLifetime)
            {
                MetadataCache.Remove(path);
                return false;
            }

            name = cached.Name;
            isFolder = cached.IsFolder;
            size = cached.Size;
            modified = cached.Modified;
            typeText = cached.TypeText;
            return true;
        }
    }

    private static void CacheMetadata(string path, string name, bool isFolder, long size,
        DateTime? modified, string typeText)
    {
        if (string.IsNullOrEmpty(path))
            return;

        lock (MetadataCacheLock)
        {
            if (MetadataCache.Count > 64)
            {
                var now = DateTime.UtcNow;

                foreach (var key in MetadataCache
                             .Where(pair => now - pair.Value.CreatedUtc > MetadataCacheLifetime)
                             .Select(pair => pair.Key)
                             .ToList())
                {
                    MetadataCache.Remove(key);
                }
            }

            MetadataCache[path] = new CachedShellMetadata
            {
                Name = name,
                IsFolder = isFolder,
                Size = size,
                Modified = modified,
                TypeText = typeText,
                CreatedUtc = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    ///     Computes the total size and content count of a shell-only folder (e.g. a folder on an MTP
    ///     device) by enumerating the shell namespace. Cancellable through <paramref name="shouldCancel" />
    ///     and never throws. Folders are not copied for this; only their metadata is read.
    /// </summary>
    public static bool TryGetShellFolderSize(string path, Func<bool> shouldCancel,
        out long totalSize, out int fileCount, out int folderCount, Action<long, int, int> progress = null)
    {
        totalSize = 0;
        fileCount = 0;
        folderCount = 0;

        dynamic item;
        dynamic parentFolder;

        try
        {
            if (!TryWalkToItem(path, out item, out parentFolder))
                return false;

            try
            {
                if (!(bool)item.IsFolder)
                    return false;

                dynamic folder = item.GetFolder;

                if (folder == null)
                    return false;

                return CountShellFolder(folder, shouldCancel, progress, 0, ref totalSize, ref fileCount, ref folderCount);
            }
            finally
            {
                ReleaseComObject(item);
                ReleaseComObject(parentFolder);
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool CountShellFolder(dynamic folder, Func<bool> shouldCancel, Action<long, int, int> progress, int depth,
        ref long totalSize, ref int fileCount, ref int folderCount)
    {
        if (depth > 32 || (shouldCancel != null && shouldCancel()))
            return false;

        try
        {
            dynamic items = folder.Items();
            var count = (int)items.Count;

            for (var i = 0; i < count; i++)
            {
                if (shouldCancel != null && shouldCancel())
                    return false;

                // Safety limit: never let a huge device folder block the preview forever.
                if (fileCount + folderCount > 50000)
                    return false;

                dynamic child = null;

                try
                {
                    child = items.Item(i);

                    if ((bool)child.IsFolder)
                    {
                        folderCount++;

                        dynamic subFolder = child.GetFolder;

                        if (subFolder != null
                         && !CountShellFolder(subFolder, shouldCancel, progress, depth + 1,
                                ref totalSize, ref fileCount, ref folderCount))
                        {
                            // The child enumeration failed or was cancelled: propagate an incomplete
                            // result so callers do not present the partial total as a final size.
                            return false;
                        }
                    }
                    else
                    {
                        fileCount++;

                        var sizeText = (string)GetDetailsString(folder, child, 2);
                        var childSize = ParseHumanSize(sizeText);

                        if (childSize > 0)
                            totalSize += childSize;
                    }
                }
                catch (Exception)
                {
                    // Broken namespace item: skip it.
                }
                finally
                {
                    ReleaseComObject(child);
                }

                if (progress != null && (fileCount + folderCount) % 128 == 0)
                {
                    try
                    {
                        progress(totalSize, fileCount, folderCount);
                    }
                    catch (Exception)
                    {
                        // Progress reporting must never break the calculation.
                    }
                }
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string GetDetailsString(dynamic folder, dynamic item, int column)
    {
        try
        {
            if (folder == null || item == null)
                return null;

            return (string)folder.GetDetailsOf(item, column);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    ///     Converts an Explorer size column value ("182 MB", "4.09 KB", ...) into bytes. Returns -1 when
    ///     the value is empty or not a size.
    /// </summary>
    private static long ParseHumanSize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return -1;

        try
        {
            var match = Regex.Match(text, @"([0-9][0-9.,\s]*)\s*([KMGTPE]?B|bytes?|[KMGTPE]|B)\b",
                RegexOptions.IgnoreCase);

            if (!match.Success)
                return -1;

            var numberText = match.Groups[1].Value.Trim();
            var unit = match.Groups[2].Value.ToUpperInvariant();

            if (!TryParseLocalizedNumber(numberText, out var value))
                return -1;

            var exponent = unit.StartsWith("K") ? 1
                : unit.StartsWith("M") ? 2
                : unit.StartsWith("G") ? 3
                : unit.StartsWith("T") ? 4
                : unit.StartsWith("P") ? 5
                : unit.StartsWith("E") ? 6
                : 0;

            if (unit.StartsWith("B") && unit.Length > 1)
                exponent = 0;

            return (long)(value * Math.Pow(1024, exponent));
        }
        catch (Exception)
        {
            return -1;
        }
    }

    /// <summary>
    ///     Parses a number rendered by an Explorer column while preserving the decimal separator of the
    ///     current culture ("4,09 KB" must stay 4.09 and never become 409). Falls back to the other
    ///     common separators only when the culture-based parse fails.
    /// </summary>
    private static bool TryParseLocalizedNumber(string text, out double value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        const NumberStyles styles = NumberStyles.Float | NumberStyles.AllowThousands;

        if (double.TryParse(text, styles, CultureInfo.CurrentCulture, out value))
            return true;

        var numberFormat = CultureInfo.CurrentCulture.NumberFormat;
        var groupsRemoved = text.Replace(numberFormat.NumberGroupSeparator, string.Empty);

        if (double.TryParse(groupsRemoved, styles, CultureInfo.CurrentCulture, out value))
            return true;

        // The column may use a decimal separator that differs from the current culture. Try both
        // interpretations, but only when they are unambiguous.
        var dotAsDecimal = groupsRemoved.Replace(",", string.Empty);

        if (double.TryParse(dotAsDecimal, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;

        var commaAsDecimal = groupsRemoved.Replace(".", string.Empty).Replace(',', '.');

        return double.TryParse(commaAsDecimal, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static dynamic FindPrefixItem(dynamic folder, string path)
    {
        try
        {
            dynamic items = folder.Items();
            var count = (int)items.Count;

            for (var i = 0; i < count; i++)
            {
                dynamic item = null;

                try
                {
                    item = items.Item(i);
                    string itemPath = item.Path;

                    if (string.IsNullOrEmpty(itemPath) || !(bool)item.IsFolder)
                    {
                        ReleaseComObject(item);
                        item = null;
                        continue;
                    }

                    var endsWithSeparator = itemPath.EndsWith("\\", StringComparison.Ordinal)
                                         || itemPath.EndsWith("/", StringComparison.Ordinal);

                    if (path.StartsWith(itemPath, StringComparison.OrdinalIgnoreCase)
                     && (endsWithSeparator || (path.Length > itemPath.Length && path[itemPath.Length] == '\\'))
                     && path.Length > itemPath.Length)
                    {
                        return item; // caller owns the COM object
                    }
                }
                catch (Exception)
                {
                    // Some namespace items throw when their path is queried; skip them.
                }

                ReleaseComObject(item);
            }
        }
        catch (Exception)
        {
            // ignored
        }

        return null;
    }

    private static dynamic FindExactItem(dynamic folder, string wantedPath)
    {
        try
        {
            dynamic items = folder.Items();
            var count = (int)items.Count;

            for (var i = 0; i < count; i++)
            {
                dynamic item = null;

                try
                {
                    item = items.Item(i);
                    string itemPath = item.Path;

                    if (string.Equals(itemPath, wantedPath, StringComparison.OrdinalIgnoreCase))
                        return item; // caller owns the COM object
                }
                catch (Exception)
                {
                    // ignore broken items
                }

                ReleaseComObject(item);
            }
        }
        catch (Exception)
        {
            // ignored
        }

        return null;
    }

    private static string TryMaterializeViaShellItem(string path, CancellationToken cancellationToken,
        Action<long, long> progress)
    {
        var tempDirectory = null as string;

        try
        {
            // Only copy items that the shell positively classifies as a file/stream. When the
            // classification fails the item must not reach IFileOperation: it may be a folder, and copying
            // a folder would transfer its whole content.
            if (!ShellItemInfo.TryGetIsFolder(path, out var isFolder) || isFolder)
                return null;

            if (ShellItemInfo.TryGetSize(path, out var size) && size > MaxMaterializedSize)
                return null;

            if (cancellationToken.IsCancellationRequested)
                return null;

            CleanupStaleTempFilesOnce();

            var safeName = MakeSafeFileName(ShellItemInfo.GetDisplayNameOrNull(path));

            if (string.IsNullOrEmpty(safeName))
                safeName = MakeSafeFileName(GetNameFromRawPath(path));

            if (string.IsNullOrEmpty(safeName))
                safeName = "shellitem";

            if (Path.GetExtension(safeName).Length == 0)
            {
                var extension = GetExtensionFromRawPath(path);

                if (!string.IsNullOrEmpty(extension) && safeName.Length + extension.Length <= 120)
                    safeName += extension;
            }

            Directory.CreateDirectory(TempRoot);

            tempDirectory = Path.Combine(TempRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            var destinationFile = Path.Combine(tempDirectory, safeName);

            // IFileOperation reports completion synchronously, but it cannot be aborted: check the
            // cancellation before and after the copy and discard the temporary file when superseded.
            if (!CopyShellItem(path, tempDirectory, safeName))
            {
                TryDeleteDirectory(tempDirectory);
                return null;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                ScheduleTempDirectoryCleanup(tempDirectory);
                return null;
            }

            if (!File.Exists(destinationFile))
            {
                // The shell may have changed the destination name; accept whatever single file landed in the folder.
                var files = Directory.GetFiles(tempDirectory);

                if (files.Length == 0)
                {
                    TryDeleteDirectory(tempDirectory);
                    return null;
                }

                destinationFile = files[0];
            }

            lock (SyncRoot)
                ActiveTempFiles.Add(destinationFile);

            return destinationFile;
        }
        catch (Exception)
        {
            if (!string.IsNullOrEmpty(tempDirectory))
                ScheduleTempDirectoryCleanup(tempDirectory);

            return null;
        }
    }

    /// <summary>
    ///     Fallback for shell items whose parsing name cannot be resolved by SHCreateItemFromParsingName
    ///     (typically items of an MTP/portable device, e.g. a photo on a phone). When the item is the
    ///     current Explorer selection - which is exactly the QuickLook preview flow - Shell.Application
    ///     exposes the selected FolderItem and the shell can copy it into a temporary file.
    /// </summary>
    private static string TryMaterializeViaExplorerSelection(string path, long expectedSize,
        bool expectedSizeIsExact, CancellationToken cancellationToken, Action<long, long> progress)
    {
        var tempDirectory = null as string;

        try
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            var shellType = Type.GetTypeFromProgID("Shell.Application");

            if (shellType == null)
                return null;

            dynamic shell = Activator.CreateInstance(shellType);
            dynamic windows = shell.Windows();
            dynamic sourceItem = null;
            string sourceSizeText = null;

            var windowCount = (int)windows.Count;

            for (var i = 0; i < windowCount && sourceItem == null; i++)
            {
                dynamic window = null;

                try
                {
                    window = windows.Item(i);

                    dynamic document = window.Document;
                    dynamic selectedItems = document.SelectedItems();

                    var itemCount = (int)selectedItems.Count;

                    for (var j = 0; j < itemCount; j++)
                    {
                        dynamic candidate = selectedItems.Item(j);
                        string candidatePath = candidate.Path;

                        if (string.Equals(candidatePath, path, StringComparison.OrdinalIgnoreCase))
                        {
                            sourceItem = candidate;

                            try
                            {
                                sourceSizeText = (string)document.Folder.GetDetailsOf(candidate, 2);
                            }
                            catch (Exception)
                            {
                                sourceSizeText = null;
                            }

                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    // Not a shell window (or the shell recycled it while enumerating).
                }
                finally
                {
                    ReleaseComObject(window);
                }
            }

            if (sourceItem == null)
                return null;

            // A folder must never be copied: it would recursively transfer all of its content.
            if ((bool)sourceItem.IsFolder)
                return null;

            if (expectedSize <= 0)
            {
                expectedSize = ParseHumanSize(sourceSizeText);
                expectedSizeIsExact = false;
            }

            if (expectedSize > MaxMaterializedSize)
                return null;

            if (cancellationToken.IsCancellationRequested)
                return null;

            string safeName = MakeSafeFileName((string)sourceItem.Name);

            if (string.IsNullOrEmpty(safeName))
                safeName = "shellitem";

            Directory.CreateDirectory(TempRoot);

            tempDirectory = Path.Combine(TempRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            dynamic destination = shell.NameSpace(tempDirectory);

            if (destination == null)
            {
                TryDeleteDirectory(tempDirectory);
                return null;
            }

            const int flags = 0x14; // FOF_SILENT | FOF_NOCONFIRMATION
            destination.CopyHere(sourceItem, flags);

            var destinationFile = Path.Combine(tempDirectory, safeName);

            if (!WaitForCopyToFinish(tempDirectory, expectedSize, expectedSizeIsExact, cancellationToken,
                    progress, out destinationFile))
            {
                ScheduleTempDirectoryCleanup(tempDirectory);
                return null;
            }

            lock (SyncRoot)
                ActiveTempFiles.Add(destinationFile);

            return destinationFile;
        }
        catch (Exception)
        {
            if (!string.IsNullOrEmpty(tempDirectory))
                ScheduleTempDirectoryCleanup(tempDirectory);

            return null;
        }
    }

    /// <summary>
    ///     Shell copy operations (Folder.CopyHere) are asynchronous; wait until the copied file exists and
    ///     its size stops growing. Bounded so a stuck device copy can never hang the viewer forever.
    /// </summary>
    private static bool WaitForCopyToFinish(string directory, long expectedSize, bool expectedSizeIsExact,
        CancellationToken cancellationToken, Action<long, long> progress, out string resultFile)
    {
        resultFile = null;

        // Folder.CopyHere does not report completion. When the shell knows the item size, wait until the
        // destination reaches that size; otherwise fall back to a stable-size heuristic. In both cases a
        // timeout is a failure: a partial file must never be handed to a preview plugin.
        var timeout = expectedSize > 0 ? TimeSpan.FromMinutes(15) : TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();
        long lastSize = -1;
        var stableSamples = 0;

        while (stopwatch.Elapsed < timeout)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            string[] files;

            try
            {
                files = Directory.GetFiles(directory);
            }
            catch (Exception)
            {
                return false;
            }

            if (files.Length > 0)
            {
                var candidate = files[0];
                long size = 0;

                try
                {
                    size = new FileInfo(candidate).Length;
                }
                catch (Exception)
                {
                    size = 0;
                }

                if (progress != null)
                {
                    try
                    {
                        progress(size, expectedSize);
                    }
                    catch (Exception)
                    {
                        // Progress reporting must never break the copy.
                    }
                }

                resultFile = candidate;

                if (expectedSize > 0)
                {
                    if (size >= expectedSize)
                    {
                        stableSamples++;

                        // With the exact PKEY_Size a short flush window is enough; for the rounded display
                        // value wait longer so a still-growing transfer is not accepted as complete.
                        if (stableSamples >= (expectedSizeIsExact ? 2 : 8))
                            return true;
                    }
                    else
                    {
                        stableSamples = 0;
                    }
                }
                else if (size > 0 && size == lastSize)
                {
                    stableSamples++;

                    if (stableSamples >= 8) // ~2 s without any progress
                        return true;
                }
                else
                {
                    stableSamples = 0;
                }

                lastSize = size;
            }

            Thread.Sleep(250);
        }

        // The copy did not finish in time; never report the (possibly partial) destination as success.
        resultFile = null;
        return false;
    }

    /// <summary>
    ///     Deletes a temporary directory after a shell copy into it has stopped writing. Folder.CopyHere
    ///     runs asynchronously, so a cancelled or failed copy may still be using the directory; deleting it
    ///     immediately would either fail or make the shell recreate files. Runs on a background thread.
    /// </summary>
    private static void ScheduleTempDirectoryCleanup(string tempDirectory)
    {
        if (string.IsNullOrEmpty(tempDirectory) || !IsUnderTempRoot(tempDirectory))
            return;

        try
        {
            var thread = new Thread(() =>
            {
                var deadline = DateTime.UtcNow.AddMinutes(10);
                long lastSize = -1;
                var stableSamples = 0;
                var emptySamples = 0;

                while (DateTime.UtcNow < deadline)
                {
                    try
                    {
                        var files = Directory.GetFiles(tempDirectory);
                        long size = 0;

                        if (files.Length > 0)
                        {
                            try
                            {
                                size = new FileInfo(files[0]).Length;
                            }
                            catch (Exception)
                            {
                                size = 0;
                            }
                        }

                        if (files.Length == 0)
                        {
                            emptySamples++;

                            if (emptySamples >= 8) // no copy ever started; remove the empty directory
                            {
                                TryDeleteDirectory(tempDirectory);

                                if (!Directory.Exists(tempDirectory))
                                    return;

                                emptySamples = 0;
                            }
                        }
                        else if (size > 0 && size == lastSize)
                        {
                            stableSamples++;

                            if (stableSamples >= 8) // ~2 s without progress: the transfer is over
                            {
                                TryDeleteDirectory(tempDirectory);

                                if (!Directory.Exists(tempDirectory))
                                    return;

                                stableSamples = 0;
                            }
                        }
                        else
                        {
                            stableSamples = 0;
                        }

                        lastSize = size;
                    }
                    catch (Exception)
                    {
                        // The shell may still hold the directory; retry until the cleanup deadline.
                        TryDeleteDirectory(tempDirectory);

                        if (!Directory.Exists(tempDirectory))
                            return;

                        Thread.Sleep(250);
                        continue;
                    }

                    Thread.Sleep(250);
                }

                TryDeleteDirectory(tempDirectory);
            })
            { IsBackground = true, Name = "QuickLook shell item cleanup" };

            thread.Start();
        }
        catch (Exception)
        {
            // Best effort only.
        }
    }

    /// <summary>
    ///     Deletes a file produced by <see cref="TryMaterialize" /> together with its temporary folder.
    ///     Only paths inside the QuickLook temporary root are touched; never throws.
    /// </summary>
    public static void TryDeleteTempFile(string tempFile)
    {
        if (string.IsNullOrEmpty(tempFile))
            return;

        try
        {
            lock (SyncRoot)
                ActiveTempFiles.Remove(tempFile);

            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(tempFile);
            }
            catch (Exception)
            {
                return;
            }

            if (!IsUnderTempRoot(fullPath))
                return;

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(directory))
                TryDeleteDirectory(directory);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    /// <summary>
    ///     Removes temporary copies left behind by a previous QuickLook session. Files that are still in
    ///     use by the current session are preserved; never throws.
    /// </summary>
    public static void CleanupStaleTempFiles()
    {
        lock (SyncRoot)
            CleanupStaleTempFilesCore();
    }

    private static void CleanupStaleTempFilesOnce()
    {
        lock (SyncRoot)
        {
            if (_staleCleanupDone)
                return;

            _staleCleanupDone = true;
            CleanupStaleTempFilesCore();
        }
    }

    private static void CleanupStaleTempFilesCore()
    {
        try
        {
            if (!Directory.Exists(TempRoot))
                return;

            var cutoff = DateTime.UtcNow - TimeSpan.FromDays(1);

            foreach (var directory in Directory.GetDirectories(TempRoot))
            {
                try
                {
                    var inUse = false;
                    var files = Directory.GetFiles(directory);

                    if (files.Length == 0)
                    {
                        TryDeleteDirectory(directory);
                        continue;
                    }

                    foreach (var file in files)
                    {
                        if (!ActiveTempFiles.Contains(file))
                            continue;

                        inUse = true;
                        break;
                    }

                    if (inUse)
                        continue;

                    if (Directory.GetLastWriteTimeUtc(directory) < cutoff)
                        TryDeleteDirectory(directory);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private static bool CopyShellItem(string sourcePath, string destinationFolder, string copyName)
    {
        IShellItem sourceItem = null;
        IShellItem destinationItem = null;
        IFileOperation fileOperation = null;

        try
        {
            if (!TryCreateShellItem(sourcePath, out sourceItem))
                return false;

            if (!TryCreateShellItem(destinationFolder, out destinationItem))
                return false;

            if (!TryCreateFileOperation(out fileOperation))
                return false;

            const uint flags = FOF_SILENT | FOF_NOCONFIRMATION | FOF_NOCONFIRMMKDIR | FOF_NOERRORUI;

            if (fileOperation.SetOperationFlags(flags) != 0)
                return false;

            // No owner window: the operation must be completely silent.
            fileOperation.SetOwnerWindow(IntPtr.Zero);

            if (fileOperation.CopyItem(sourceItem, destinationItem, copyName, IntPtr.Zero) != 0)
                return false;

            if (fileOperation.PerformOperations() != 0)
                return false;

            fileOperation.GetAnyOperationsAborted(out var aborted);
            return aborted == 0;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            ReleaseComObject(fileOperation);
            ReleaseComObject(destinationItem);
            ReleaseComObject(sourceItem);
        }
    }

    private static bool TryCreateShellItem(string path, out IShellItem shellItem)
    {
        shellItem = null;

        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var riid = new Guid(IID_IShellItem);
            var hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref riid, out var item);

            if (hr < 0 || item == null)
                return false;

            shellItem = item;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryCreateFileOperation(out IFileOperation fileOperation)
    {
        fileOperation = null;

        try
        {
            var clsid = new Guid(CLSID_FileOperation);
            var riid = new Guid(IID_IFileOperation);
            var hr = CoCreateInstance(ref clsid, IntPtr.Zero, ClsCtxAll, ref riid, out var operation);

            if (hr < 0 || operation == null)
                return false;

            fileOperation = operation;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string MakeSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);

        foreach (var c in name)
        {
            builder.Append(Array.IndexOf(invalidChars, c) >= 0 ? '_' : c);
        }

        var safe = builder.ToString().Trim().TrimEnd('.');

        if (safe.Length == 0)
            return null;

        var baseName = Path.GetFileNameWithoutExtension(safe);

        if (ReservedFileNames.Contains(baseName))
            safe = "_" + safe;

        if (safe.Length > 120)
        {
            var extension = Path.GetExtension(safe);

            if (extension.Length > 20)
                extension = string.Empty;

            var baseLength = 120 - extension.Length;

            if (baseLength < 1)
                baseLength = 1;

            safe = safe.Substring(0, baseLength) + extension;
        }

        return safe;
    }

    private static string GetNameFromRawPath(string path)
    {
        try
        {
            var name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(name))
                return name;
        }
        catch (Exception)
        {
            // System.IO rejects the parsing name, fall through to plain string handling.
        }

        try
        {
            var trimmed = path.TrimEnd('\\', '/');
            var separator = Math.Max(trimmed.LastIndexOf('\\'), trimmed.LastIndexOf('/'));
            var candidate = separator >= 0 ? trimmed.Substring(separator + 1) : trimmed;

            if (candidate.StartsWith("::", StringComparison.Ordinal))
                candidate = candidate.Substring(2);

            return candidate.IndexOf('.') > 0 ? candidate : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string GetExtensionFromRawPath(string path)
    {
        try
        {
            var raw = path?.TrimEnd('\\', '/');

            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            var separator = Math.Max(raw.LastIndexOf('\\'), raw.LastIndexOf('/'));
            var name = separator >= 0 ? raw.Substring(separator + 1) : raw;
            var dot = name.LastIndexOf('.');

            if (dot <= 0 || dot == name.Length - 1)
                return string.Empty;

            var extension = name.Substring(dot);

            if (extension.Length > 20)
                return string.Empty;

            foreach (var c in extension)
            {
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '_' && c != '-')
                    return string.Empty;
            }

            return extension;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    /// <summary>
    ///     True for paths whose whole meaning is one shell namespace root, e.g. "::{645FF040-5081-101B-9F08-00AA002F954E}"
    ///     (Recycle Bin) or "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}" (This PC). Those are handled by CLSIDViewer.
    /// </summary>
    public static bool IsExactClsidPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var trimmed = path.Trim();

        if (!trimmed.StartsWith("::", StringComparison.Ordinal))
            return false;

        if (trimmed.IndexOf('\\') >= 0 || trimmed.IndexOf('/') >= 0)
            return false;

        if (trimmed.Length < 5 || trimmed[2] != '{' || trimmed[trimmed.Length - 1] != '}')
            return false;

        return Guid.TryParse(trimmed.Substring(3, trimmed.Length - 4), out _);
    }

    private static bool IsUnderTempRoot(string fullPath)
    {
        var root = TempRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   + Path.DirectorySeparatorChar;

        return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)
               || string.Equals(fullPath, TempRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private static void ReleaseComObject(object comObject)
    {
        if (comObject == null)
            return;

        try
        {
            if (Marshal.IsComObject(comObject))
                Marshal.ReleaseComObject(comObject);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private const string CLSID_FileOperation = "3AD05575-8857-4850-9277-11B85BDB8E09";
    private const string IID_IFileOperation = "947AAB5F-0A5C-4C13-B4D6-4BF7836FC9F8";
    private const string IID_IShellItem = "43826D1E-E718-42EE-BC55-A1E261C37BFE";

    private const uint ClsCtxAll = 0x17;

    private const uint FOF_SILENT = 0x0004;
    private const uint FOF_NOCONFIRMATION = 0x0010;
    private const uint FOF_NOCONFIRMMKDIR = 0x0200;
    private const uint FOF_NOERRORUI = 0x0400;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(
        ref Guid rclsid,
        IntPtr pUnkOuter,
        uint dwClsContext,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IFileOperation ppv);

    [ComImport]
    [Guid(IID_IShellItem)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        [PreserveSig]
        int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

        [PreserveSig]
        int GetParent(out IntPtr ppsi);

        [PreserveSig]
        int GetDisplayName(uint sigdnName, out IntPtr ppszName);

        [PreserveSig]
        int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

        [PreserveSig]
        int Compare(IntPtr psi, uint hint, out int piOrder);
    }

    [ComImport]
    [Guid(IID_IFileOperation)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOperation
    {
        [PreserveSig]
        int Advise(IntPtr pfops, out uint pdwCookie);

        [PreserveSig]
        int Unadvise(uint dwCookie);

        [PreserveSig]
        int SetOperationFlags(uint dwOperationFlags);

        [PreserveSig]
        int SetProgressMessage([MarshalAs(UnmanagedType.LPWStr)] string pszMessage);

        [PreserveSig]
        int SetProgressDialog(IntPtr popdlg);

        [PreserveSig]
        int SetProperties(IntPtr pproparray);

        [PreserveSig]
        int SetOwnerWindow(IntPtr hwndOwner);

        [PreserveSig]
        int ApplyPropertiesToItem(IntPtr psiItem);

        [PreserveSig]
        int ApplyPropertiesToItems(IntPtr punkItems);

        [PreserveSig]
        int RenameItem(IntPtr psiItem, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IntPtr pfopsItem);

        [PreserveSig]
        int RenameItems(IntPtr pUnkItems, [MarshalAs(UnmanagedType.LPWStr)] string pszNewName);

        [PreserveSig]
        int MoveItem(IntPtr psiItem, IntPtr psiDestinationFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string pszNewName, IntPtr pfopsItem);

        [PreserveSig]
        int MoveItems(IntPtr punkItems, IntPtr psiDestinationFolder);

        [PreserveSig]
        int CopyItem(IShellItem psiItem, IShellItem psiDestinationFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string pszCopyName, IntPtr pfopsItem);

        [PreserveSig]
        int CopyItems(IntPtr punkItems, IntPtr psiDestinationFolder);

        [PreserveSig]
        int DeleteItem(IntPtr psiItem, IntPtr pfopsItem);

        [PreserveSig]
        int DeleteItems(IntPtr punkItems);

        [PreserveSig]
        int NewItem(IntPtr psiDestinationFolder, uint dwFileAttributes,
            [MarshalAs(UnmanagedType.LPWStr)] string pszName,
            [MarshalAs(UnmanagedType.LPWStr)] string pszTemplateName, IntPtr pfopsItem);

        [PreserveSig]
        int PerformOperations();

        [PreserveSig]
        int GetAnyOperationsAborted(out int pfAnyOperationsAborted);
    }
}
