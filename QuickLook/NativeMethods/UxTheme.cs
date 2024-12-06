using Microsoft.Win32;
using QuickLook.Common.Helpers;
using System;
using System.Runtime.InteropServices;

namespace QuickLook.NativeMethods;

internal static class UxTheme
{
    [DllImport("uxtheme.dll", EntryPoint = "#132", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool ShouldAppsUseDarkMode();

    /// <summary>
    /// Windows 10 1903, aka 18362, broke the API.
    ///  - Before 18362, the #135 is AllowDarkModeForApp(BOOL)
    ///  - After 18362, the #135 is SetPreferredAppMode(PreferredAppMode)
    /// Since the support for AllowDarkModeForApp is uncertain, it will not be considered for use.
    /// </summary>
    [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int SetPreferredAppMode(PreferredAppMode preferredAppMode);

    [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true, CharSet = CharSet.Unicode)]
    [Obsolete("Since the support for AllowDarkModeForApp is uncertain, it will not be considered for use.")]
    public static extern void AllowDarkModeForApp(bool allowDark);

    [DllImport("uxtheme.dll", EntryPoint = "#136", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern void FlushMenuThemes();

    [DllImport("uxtheme.dll", EntryPoint = "#138", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool ShouldSystemUseDarkMode();

    public enum PreferredAppMode : int { Default, AllowDark, ForceDark, ForceLight, Max };

    public static void ApplyPreferredAppMode()
    {
        // Enable dark mode for context menus if using dark theme
        if (Environment.OSVersion.Version.Build >= 18362) // Windows 10 1903
        {
            // UxTheme methods will apply all of menus.
            // However, the Windows style system prefers that
            // Windows System Menu is based on `OSThemeHelper.AppsUseDarkTheme`,
            // and Tray Context Menu is based on `OSThemeHelper.SystemUsesDarkTheme` when using a custom theme.
            // But actually we can't have our cake and eat it too.
            // Finally, we synchronize the theme styles of tray with higher usage rates.
            if (OSThemeHelper.SystemUsesDarkTheme())
            {
                SetPreferredAppMode(PreferredAppMode.ForceDark);
                FlushMenuThemes();
            }

            // Synchronize the theme with system settings
            SystemEvents.UserPreferenceChanged += (_, _) =>
            {
                if (OSThemeHelper.SystemUsesDarkTheme())
                {
                    SetPreferredAppMode(PreferredAppMode.ForceDark);
                    FlushMenuThemes();
                }
                else
                {
                    SetPreferredAppMode(PreferredAppMode.ForceLight);
                    FlushMenuThemes();
                }
            };
        }
    }
}
