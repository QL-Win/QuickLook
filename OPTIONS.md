# OPTIONS

This document summarizes the current repository's advanced configuration options.
All options are stored in XML config files under QuickLook data location:

- `%APPDATA%\pooi.moe\QuickLook\` for installed mode
- `UserData\` next to the executable for portable mode

Each config file is named after its domain, e.g. `QuickLook.config`, `QuickLook.Plugin.ImageViewer.config`, `QuickLook.Plugin.VideoViewer.config`.

## Config file format

The config file is a simple XML document. If the file does not exist, run QuickLook once and then create it.

Example:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Settings>
  <ShowTrayIcon>True</ShowTrayIcon>
  <UseTransparency>True</UseTransparency>
</Settings>
```

## QuickLook.config options

### `<ShowTrayIcon>`
- Default: `True`
- Type: `Boolean`
- Description: Show or hide the tray icon.
- Example:
  - `<ShowTrayIcon>False</ShowTrayIcon>` to hide the tray icon.

### `<UseTransparency>`
- Default: `True`
- Type: `Boolean`
- Description: Enable or disable window transparency for the preview window.
- Example:
  - `<UseTransparency>False</UseTransparency>` to disable transparency.

### `<WindowBackdrop>`
- Default: `Auto`
- Type: `String` (enum)
- Description: Choose the backdrop style for the preview window when transparency is enabled.
- Supported values:
  - `Auto`
  - `None`
  - `Mica`
  - `Acrylic`
  - `Acrylic10`
  - `Acrylic11`
  - `Tabbed`
- Example:
  - `<WindowBackdrop>Acrylic</WindowBackdrop>`

### `<WindowBackgroundColor>`
- Default: empty
- Type: `String`
- Description: Custom preview window background color. The value is parsed by WPF `BrushConverter`.
- Example:
  - `<WindowBackgroundColor>#FFC0CB</WindowBackgroundColor>`

### `<Topmost>`
- Default: `False`
- Type: `Boolean`
- Description: Keep the preview window on top of other windows.
- Example:
  - `<Topmost>True</Topmost>`

### `<ShowInTaskbar>`
- Default: `False`
- Type: `Boolean`
- Description: Show or hide the preview window in the taskbar.
- Example:
  - `<ShowInTaskbar>True</ShowInTaskbar>`

### `<CloseOnLostFocus>`
- Default: `False`
- Type: `Boolean`
- Description: Close the preview window when it loses focus.
- Example:
  - `<CloseOnLostFocus>True</CloseOnLostFocus>`

### `<ShowReload>`
- Default: `False`
- Type: `Boolean`
- Description: Show the Reload button in the preview window UI.
- Example:
  - `<ShowReload>True</ShowReload>`

### `<AutoReload>`
- Default: `False`
- Type: `Boolean`
- Description: Automatically reload the preview when the opened file changes on disk.
- Example:
  - `<AutoReload>True</AutoReload>`

### `<FocusWindowOnOpen>`
- Default: `False`
- Type: `Boolean`
- Description: Activate the preview window when a file is opened.
- Example:
  - `<FocusWindowOnOpen>True</FocusWindowOnOpen>`

### `<DisableAutoUpdateCheck>`
- Default: `False`
- Type: `Boolean`
- Description: Disable automatic update checks at startup.
- Example:
  - `<DisableAutoUpdateCheck>True</DisableAutoUpdateCheck>`

### `<LastUpdateTicks>`
- Default: none / internal
- Type: `Int64`
- Description: Internal timestamp used to throttle automatic update checks; not usually edited by hand.
- Example:
  - `<LastUpdateTicks>637xxxxxxx000000000</LastUpdateTicks>`

### `<ProcessRenderMode>`
- Default: `0` (`RenderMode.Default`)
- Type: `Integer`
- Description: Set process render mode at startup.
  - `0` = default rendering behavior
  - `1` = software-only rendering
- Example:
  - `<ProcessRenderMode>1</ProcessRenderMode>`

### `<TriedUnblock>`
- Default: `False`
- Type: `Boolean`
- Description: Internal flag used by plugin unblock logic after a security block attempt; not normally modified manually.
- Example:
  - `<TriedUnblock>True</TriedUnblock>`

### Extension filter options
These keys are also stored in `QuickLook.config`.

#### `<UseExtensionAllowlist>`
- Default: `False`
- Type: `Boolean`
- Description: When `True`, only extensions listed in `ExtensionAllowlist` are allowed for preview. When `False`, `ExtensionBlocklist` is used instead.
- Example:
  - `<UseExtensionAllowlist>True</UseExtensionAllowlist>`

#### `<ExtensionAllowlist>`
- Default: empty
- Type: `String`
- Description: Semicolon/comma-separated list of allowed file extensions in allowlist mode. Use leading dots, e.g. `.txt;.md`.
- Example:
  - `<ExtensionAllowlist>.txt;.md;.json</ExtensionAllowlist>`

#### `<ExtensionBlocklist>`
- Default: contains `.insv`
- Type: `String`
- Description: Semicolon/comma-separated list of blocked file extensions in blocklist mode. Use leading dots.
- Example:
  - `<ExtensionBlocklist>.insv;.exe</ExtensionBlocklist>`

## QuickLook.Plugin.ImageViewer.config options

### `<UseColorProfile>`
- Default: `False`
- Type: `Boolean`
- Description: Enable monitor color profile conversion for image preview. This may slow down image loading.
- Example:
  - `<UseColorProfile>True</UseColorProfile>`

### `<UseNativeProvider>`
- Default: `True`
- Type: `Boolean`
- Description: Use the native image provider for faster but less precise color output. Set to `False` for more accurate colors.
- Example:
  - `<UseNativeProvider>False</UseNativeProvider>`

### `<RenderSvgWeb>`
- Default: `True`
- Type: `Boolean`
- Description: Enable SVG rendering through the ImageViewer webview handler.
- Example:
  - `<RenderSvgWeb>False</RenderSvgWeb>`

### `<LastTheme>`
- Default: `1` (`Dark`)
- Type: `Integer`
- Description: Remember the last theme used by the ImageViewer web preview.
  - `0` = None
  - `1` = Dark
  - `2` = Light
- Example:
  - `<LastTheme>2</LastTheme>`

## QuickLook.Plugin.VideoViewer.config options

### `<ShouldLoop>`
- Default: `False`
- Type: `Boolean`
- Description: Loop video playback when the video reaches the end.
- Example:
  - `<ShouldLoop>True</ShouldLoop>`

### `<VolumeDouble>`
- Default: `1.0`
- Type: `Double`
- Description: Saved volume level for video playback. Value is clamped between `0.0` and `1.0`.
- Example:
  - `<VolumeDouble>0.75</VolumeDouble>`

## QuickLook.Plugin.OfficeViewer.config options

### `<CheckPreviewHandler>`
- Default: `True`
- Type: `Boolean`
- Description: Check the registered Office preview handler before loading OfficeViewer-Native.
- Example:
  - `<CheckPreviewHandler>False</CheckPreviewHandler>`

### `<AlwaysUnblockProtectedView>`
- Default: `False`
- Type: `Boolean`
- Description: Automatically unblock Protected View Internet zone identifiers for Office files without prompting.
- Example:
  - `<AlwaysUnblockProtectedView>True</AlwaysUnblockProtectedView>`

## QuickLook.Plugin.TextViewer.config options

### `<UseFormatDetector>`
- Default: `True`
- Type: `Boolean`
- Description: Enable format detection for the text viewer to improve syntax highlighting choice.
- Example:
  - `<UseFormatDetector>False</UseFormatDetector>`

### `<AllowDarkTheme>`
- Default: default theme behavior
- Type: `Boolean`
- Description: Allow dark theme usage in the text viewer when the system is in dark mode. If disabled, light theme is used even when the theme would otherwise be dark.
- Example:
  - `<AllowDarkTheme>True</AllowDarkTheme>`

### `<FontFamily>`
- Default: default font of the current language
- Type: `String`
- Description: Allow setting of the font used in the text viewer.
- Example:
  - `<FontFamily>Cascadia Mono SemiLight</FontFamily>`

### `<FontSize>`
- Default: 14.0
- Type: `Double`
- Description: Allow setting of font size used in the text viewer.
- Example:
  - `<FontSize>13</FontSize>`

## Notes

- All option names are case-sensitive and stored as XML element names under `<Settings>`.
- Plugin option domain names correspond to config file names, e.g. `QuickLook.Plugin.ImageViewer` → `QuickLook.Plugin.ImageViewer.config`.
- `LastUpdateTicks` and `TriedUnblock` are internal state values and generally do not need manual editing.
