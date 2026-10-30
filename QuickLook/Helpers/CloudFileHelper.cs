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
using System.IO;

namespace QuickLook.Helpers;

internal static class CloudFileHelper
{
    private const int FileAttributeOffline = 0x00001000;
    private const int FileAttributeRecallOnOpen = 0x00040000;
    private const int FileAttributeRecallOnDataAccess = 0x00400000;

    internal static CloudFileInfo GetInfo(string path)
    {
        return new CloudFileInfo(IsCloudPlaceholder(path), GetProviderName(path));
    }

    internal static bool IsCloudPlaceholder(string path)
    {
        if (string.IsNullOrEmpty(path) || path.StartsWith("::", StringComparison.Ordinal))
            return false;

        try
        {
            var attributes = (int)File.GetAttributes(path);

            return (attributes & FileAttributeOffline) != 0 ||
                   (attributes & FileAttributeRecallOnOpen) != 0 ||
                   (attributes & FileAttributeRecallOnDataAccess) != 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string GetProviderName(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        var normalizedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (IsUnderKnownRoot(normalizedPath, Environment.GetEnvironmentVariable("OneDrive")) ||
            IsUnderKnownRoot(normalizedPath, Environment.GetEnvironmentVariable("OneDriveConsumer")) ||
            IsUnderKnownRoot(normalizedPath, Environment.GetEnvironmentVariable("OneDriveCommercial")) ||
            ContainsPathPart(normalizedPath, "OneDrive"))
            return "OneDrive";

        if (ContainsPathPart(normalizedPath, "Google Drive") ||
            ContainsPathPart(normalizedPath, "DriveFS") ||
            ContainsPathPart(normalizedPath, "My Drive"))
            return "Google Drive";

        if (ContainsPathPart(normalizedPath, "Dropbox"))
            return "Dropbox";

        if (ContainsPathPart(normalizedPath, "iCloudDrive") ||
            ContainsPathPart(normalizedPath, "iCloud Drive"))
            return "iCloud Drive";

        if (ContainsPathPart(normalizedPath, "Box"))
            return "Box";

        return string.Empty;
    }

    private static bool IsUnderKnownRoot(string path, string root)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(root))
            return false;

        var normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return path.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsPathPart(string path, string part)
    {
        return path.IndexOf(Path.DirectorySeparatorChar + part + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.IndexOf(Path.AltDirectorySeparatorChar + part + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.EndsWith(Path.DirectorySeparatorChar + part, StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(Path.AltDirectorySeparatorChar + part, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class CloudFileInfo
{
    internal CloudFileInfo(bool isPlaceholder, string providerName)
    {
        IsPlaceholder = isPlaceholder;
        ProviderName = providerName ?? string.Empty;
    }

    internal bool IsPlaceholder { get; }

    internal string ProviderName { get; }
}
