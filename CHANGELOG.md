## 4.2.0

- Add built-in MediaInfoViewer plugin and support it in more menu
- Add 'Copy as path' option to more menu
- Add cross-plugin 'Reopen as' menu for SVG and HTML [#1690](https://github.com/QL-Win/QuickLook/issues/1690)
- Support Point Cloud Data (.pcd) for 3D spatial (Only PCD files with the PointXYZ format are supported, while Color and Intensity formats are not.)
- Support Mermaid diagram rendering in MarkdownViewer [#1730](https://github.com/QL-Win/QuickLook/issues/1730)
- Support .pdn in ThumbnailViewer [#1708](https://github.com/QL-Win/QuickLook/issues/1708)
- Improve CLI performance [#1706](https://github.com/QL-Win/QuickLook/issues/1706) [#1731](https://github.com/QL-Win/QuickLook/issues/1731)
- Set default background to transparent for SVG panel
- Improve UI/UX of font loading
- Add diff file syntax highlighting
- Add Swedish translation [#1755](https://github.com/QL-Win/QuickLook/issues/1755)
- Add .slnx extension to XML syntax highlighting
- Add support for Telegram Sticker (.tgs) files [#1762](https://github.com/QL-Win/QuickLook/issues/1762)
- Add .snupkg and .asar support to archive viewer
- Add .krc file support to TextViewer
- Add UseNativeProvider option [#1726](https://github.com/QL-Win/QuickLook/issues/1726)
- Fix image .jxr error reading from UseColorProfile
- Fix issue where font file stays locked [#77](https://github.com/QL-Win/QuickLook/issues/77)
- Fix font file unicode name is not supported
- Fix extracting cover art will not cause the title to be lost [#1759](https://github.com/QL-Win/QuickLook/issues/1759)
- Fix HelixViewer default height being too large
- Fix long path handling issue in HtmlViewer [#1643](https://github.com/QL-Win/QuickLook/issues/1643)
- Update Batch syntax highlighting colors
- Refactor tray icon to use TrayIconHost
- Refactor to make exe-installer no forked relaunching
- Remove unimportant UnobservedTaskException [#1691](https://github.com/QL-Win/QuickLook/issues/1691)
- Remove configuration `ModernMessageBox`

## 4.1.1

- Add built-in ThumbnailViewer plugin [#1662](https://github.com/QL-Win/QuickLook/issues/1662)
- Add built-in HelixViewer for 3d models [#1662](https://github.com/QL-Win/QuickLook/issues/1662)
- Add FBX model support using AssimpNet [#1479](https://github.com/QL-Win/QuickLook/issues/1479)
- Add `SVGA` and `Lottie Files` animation preview support
- Add MathJax inline math support to Markdown [#1640](https://github.com/QL-Win/QuickLook/issues/1640)
- Add `SubRip Subtitle (.srt) files`, `Protobuf`, `NSIS`, `.gitmodules`, `.dotsettings`, `.gitignore`, `.gitattributes`, `Markdown`, `reStructuredText`, `simple QML syntax`, `.env`, `Configuration (.conf;.config;.cfg)` highlighting [#1002](https://github.com/QL-Win/QuickLook/issues/1002)
- Add dark mode highlighting for `PowerShell`, `Registry`, `C`, `C++`, `Java`, `Rust`, `SQL`, `Ruby`, `R`, `PHP`, `Pascal`, `Objective-C`, `Lisp`, `Kotlin`, `Erlang`, `Dart`, `Swift`, `VisualSolution`, `CMake`
- Add `MakefileDetector`, `CMakeListsDetector for CMakeLists.txt`, `DockerfileDetector`, `HostsDetector for hosts` for text viewer
- Improve QuickLook initialization speed
- Optimize JSONDetector with Span
- Set RichTextBox background to transparent
- Revert Add Sandbox detection from 4.1.0 which will call crash

## 4.1.0

- Add built-in AppViewer plugin for `.msi`, `.appx`, `.msix`, `.wgt`, `.wgtu`, `.apk`, `.ipa`, `.hap`, `.deb`, `.dmg`, `.appimage`, `.rpm`, `.aab`
- Add built-in ELF viewer plugin for ELF-type files
- Add reload feature by JSuttHoops but you should enable `AutoReload` option firstly
- New option ProcessRenderMode
- Use format detector feature for TextViewer, only `JSON` / `XML` available now
- Add support more highlighting for `HLSL`, `XML`, `TXT`, `Properties`, `Lyric`, `Log`, `Python`, `JavaScript`, `Vue`, `CSS`, `Go`, `YAML`, `F#`, `INI`, `TypeScript`, `VB`, `SubStation Alpha` and `Lua`
- No markdown resource extraction [#1661](https://github.com/QL-Win/QuickLook/issues/1661) [#1670](https://github.com/QL-Win/QuickLook/issues/1670)
- Support X11 and more JPEG2000 image formats
- Support JXR image but SDR only [#1680](https://github.com/QL-Win/QuickLook/issues/1680)
- Enable window dragging in video viewer panel [#425](https://github.com/QL-Win/QuickLook/issues/425)
- Add SVG support using WebView2 in ImageViewer
- Support RTL for .txt file [#1612](https://github.com/QL-Win/QuickLook/issues/1612)
- Add `Alt+Z` shortcut to toggle word wrap [#1487](https://github.com/QL-Win/QuickLook/issues/1487)
- Improve startup speed [#1521](https://github.com/QL-Win/QuickLook/issues/1521)
- Improve PDF magic detection
- Improve GroupBox UI/UX
- Attempt to fix the crash [#1648](https://github.com/QL-Win/QuickLook/issues/1648) `This is an experimental fix, the idea is to remove the tree to prevent the DUCE command`
- Update font pangram for FontViewer
- Update de translations by King3R
- Manually resolve the assembly fails [#1618](https://github.com/QL-Win/QuickLook/issues/1618)
- Merge OfficeViewer-Native plugin [#1662](https://github.com/QL-Win/QuickLook/issues/1662)
- New option CheckPreviewHandler for OfficeViewer-Native
- Add Sandbox detection
- Revert the DataGrid style of CSV [#1664](https://github.com/QL-Win/QuickLook/issues/1664)
- Remove the WoW64HookHelper from release [#1634](https://github.com/QL-Win/QuickLook/issues/1634)
- Fix share button was not visible in win11
- Fix generic theme resources [#1652](https://github.com/QL-Win/QuickLook/issues/1652)
- Fix old version volume exception [#1653](https://github.com/QL-Win/QuickLook/issues/1653)
- Fix CaptionTextButtonStyle not static anymore
- Fix unsupported ColorContexts in Windows [#1671](https://github.com/QL-Win/QuickLook/issues/1671)
- ~~Fix long path issue [#1643](https://github.com/QL-Win/QuickLook/issues/1643)~~

## 4.0.2

- Support .pcx image [#1638](https://github.com/QL-Win/QuickLook/issues/1638)
- Improve PE parsing with extended buffer size
- Fix flickering [#1628](https://github.com/QL-Win/QuickLook/issues/1628)
- Fix DpiAwareness for PerMonitor [#1626](https://github.com/QL-Win/QuickLook/issues/1626)

- Hide PEViewer Title just like InfoPanel
- Avoid audio cover null exception in xaml

## 4.0.1

- Support more Markdown file extensions [#1562](https://github.com/QL-Win/QuickLook/issues/1562), [#1601](https://github.com/QL-Win/QuickLook/issues/1601)
- Support CLI options [#1620](https://github.com/QL-Win/QuickLook/issues/1620)
- Update pt-BR translations in Translations.config
- Delay initialization of MarkdownViewer
- Make .exe installer use MSI path by default [#1596](https://github.com/QL-Win/QuickLook/issues/1596)
- Fix style issues in the Search Panel [#1592](https://github.com/QL-Win/QuickLook/issues/1592)
- Fix volume control not working [#1578](https://github.com/QL-Win/QuickLook/issues/1578)
- Fix exception when checking for updates [#1577](https://github.com/QL-Win/QuickLook/issues/1577)

## 4.0.0

- Add built-in PE viewer plugin
- Add built-in font viewer plugin
- Update translations
- Update dependent packages
- Add support for Multi Commander
- Add support for both Everything v1.4 and v1.5(a)
- Add "Open Data Folder" and dark mode support to tray menu
- Add "Restart QuickLook" option to tray menu [#1448](https://github.com/QL-Win/QuickLook/issues/1448)
- Implement modern message box UI
- Replace icons with Segoe Fluent Icons
- Detect and auto-fix Windows blocking issues [#1495](https://github.com/QL-Win/QuickLook/issues/1495)
- Adjust tray menu position
- Use MicaSetup to create EXE installer
- Fix plugin installer description length limit
- Prevent crash when WMI fails [#1379](https://github.com/QL-Win/QuickLook/issues/1379)
- Show toast when "Prevent Closing" cannot be cancelled [#1368](https://github.com/QL-Win/QuickLook/issues/1368)
- Add support for multi-layer GIMP .xcf files [#1224](https://github.com/QL-Win/QuickLook/issues/1224) for ImageViewer
- Fix .xcf file extension check [#1229](https://github.com/QL-Win/QuickLook/issues/1229) for ImageViewer
- Fix HEIC preview rendering [#1470](https://github.com/QL-Win/QuickLook/issues/1470) for ImageViewer
- Add support for .qoi, .icns, .dds, .svgz, .psb, .cur, and .ani formats for ImageViewer
- Improve animated WebP support (x64 only) [#1024](https://github.com/QL-Win/QuickLook/issues/1024) [#1324](https://github.com/QL-Win/QuickLook/issues/1324) for ImageViewer
- Improve GIF decoding performance [#993](https://github.com/QL-Win/QuickLook/issues/993) for ImageViewer
- Add copy button to image viewer [#1399](https://github.com/QL-Win/QuickLook/issues/1399) for ImageViewer
- Fix SVG rendering error [#1430](https://github.com/QL-Win/QuickLook/issues/1430) for ImageViewer
- Add double-encoding detection [#471](https://github.com/QL-Win/QuickLook/issues/471) [#600](https://github.com/QL-Win/QuickLook/issues/600) for TextViewer
- Improve dark mode rendering for TextViewer
- Catch exceptions from XSHD loader for TextViewer
- Add syntax highlighting for shell scripts [#668](https://github.com/QL-Win/QuickLook/issues/668) for TextViewer
- Add dark mode support for C# syntax highlighting for TextViewer
- Improve support for comic archive formats [#1276](https://github.com/QL-Win/QuickLook/issues/1276) for ArchiveViewer
- Redesign file list with Fluent UI for ArchiveViewer
- Change default background color to blue for CsvViewer
- Fix issue with non-UTF8 CSV encoding for CsvViewer
- Improve rendering and stability for MarkdownViewer
- Add support for password-protected PDFs [#155](https://github.com/QL-Win/QuickLook/issues/155) for PDFViewer
- Enable auto-resizing of the viewer window for PDFViewer
- Fix audio cover parsing error for multiple embedded images for VideoViewer
- Add lyric (.lrc) support for audio files [#1506](https://github.com/QL-Win/QuickLook/issues/1506) for VideoViewer
- Add support for .mid audio format [#931](https://github.com/QL-Win/QuickLook/issues/931) for VideoViewer
- Fix time label overflow in long videos for VideoViewer
