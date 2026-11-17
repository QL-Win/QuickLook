// Copyright © 2017-2026 QL-Win Contributors
// Licensed under the GNU General Public License, version 3 or later.

using QuickLook.Common.Helpers;
using System.IO;

namespace QuickLook.Plugin.ArchiveViewer.ArchiveFile;

/// <summary>
/// Accessible archive list labels translated through the plugin's existing language file.
/// </summary>
public static class ArchiveFileStrings
{
    private static readonly string TranslationFile =
        Path.Combine(Path.GetDirectoryName(typeof(ArchiveFileStrings).Assembly.Location), "Translations.config");

    public static readonly string ArchiveContents =
        TranslationHelper.Get("AV_ArchiveContents", TranslationFile, failsafe: "Archive contents");

    public static readonly string ExpandOrCollapseFolder =
        TranslationHelper.Get("AV_ExpandOrCollapseFolder", TranslationFile, failsafe: "Expand or collapse folder");

    public static readonly string LevelFormat =
        TranslationHelper.Get("AV_LevelFormat", TranslationFile, failsafe: "Level {0}");
}
