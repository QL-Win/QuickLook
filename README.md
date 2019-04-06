
![QuickLook icon](https://user-images.githubusercontent.com/1687847/29485863-8cd61b7c-84e2-11e7-97d5-eacc2ba10d28.png)

# QuickLook

[![license](https://img.shields.io/github/license/QL-Win/QuickLook.svg)](https://www.gnu.org/licenses/gpl-3.0.en.html)
[![AppVeyor](https://img.shields.io/appveyor/ci/xupefei/QuickLook.svg)](https://ci.appveyor.com/project/xupefei/QuickLook)
[![Github All Releases](https://img.shields.io/github/downloads/QL-Win/QuickLook/total.svg)](https://github.com/QL-Win/QuickLook/releases)
[![GitHub release](https://img.shields.io/github/release/QL-Win/QuickLook.svg)](https://github.com/QL-Win/QuickLook/releases/latest)

*This project is currently under heavy development. Come back often to see what's new.*

<img src="http://pooi.moe/QuickLook/sample.gif?3" width="400">

## Background
One of the few features I missed from macOS is [Quick Look](https://en.wikipedia.org/wiki/Quick_Look). It allows user peek into a file content in lightning speed by just pressing the <kbd>Space</kbd> key. Windows, on the other hand, does not have this handy feature ... until now.

I am aware that several alternatives are already available on the Internet (e.g. [WinQuickLook](https://github.com/shibayan/WinQuickLook) and [Seer](https://github.com/ccseer/Seer)). Despite these options, I still decided to craft another one by myself, because they are either not being actively developed, lack of variety, or ask for an amount of $$$.

## Highlights

 - Tons of supported file types (full list [here](https://github.com/QL-Win/QuickLook/wiki/Supported-File-Types))
 - Fluent design (new in version 0.3)
 - Touchscreen friendly
 - HiDPI support
 - Preview from *Open* and *Save File* Dialog
 - Preview from 3rd-party file managers (see a list [here](https://github.com/QL-Win/QuickLook/wiki/File-Managers))
 - Easily extended by [plugins](https://github.com/QL-Win/QuickLook/wiki/Available-Plugins)
 - Strict GPL license to keep it free forever

## Usage

### Download/Installation

Get it from one of the following sources:

  * Microsoft Store (Windows 10 users only) <a href="https://www.microsoft.com/store/apps/9nv4bs3l1h4s?ocid=badge" target="_blank"><img src="https://assets.windowsphone.com/13484911-a6ab-4170-8b7e-795c1e8b4165/English_get_L_InvariantCulture_Default.png" height="22px" alt="Store Link" /></a> 
  * Installer or portable archive of the stable version from [GitHub Release](https://github.com/QL-Win/QuickLook/releases) 
  * Nightly builds from [AppVeyor](https://ci.appveyor.com/project/xupefei/quicklook/build/artifacts)

[What are the differences between `.msi`, `.zip`, Nightly and Store versions?](https://github.com/QL-Win/QuickLook/wiki/Differences-Between-Distributions)


### Typical usecase

1. Run `QuickLook.exe` (only necessary if autostart is disabled)
1. Select any file or folder (on the Desktop, in a File Explorer window, in an *Open* or *Save-File* dialogue, doesn't matter)
1. Press <kbd>Spacebar</kbd>
1. Enjoy the preview and interact with it
1. Preview next file by clicking on it or using arrow-keys (arrow-keys move selection in the background if the preview window is not in focus)
1. When you're done close it by either hitting <kbd>Spacebar</kbd> again, pressing <kbd>Esc</kbd> or clicking the `⨉` button

### Hotkeys and buttons

 - <kbd>Spacebar</kbd> Show/Hide the preview window
 - <kbd>Esc</kbd> Hide the preview window
 - <kbd>Enter</kbd> Open/Execute current file
 - <kbd>Mouse</kbd> <kbd>↑</kbd> <kbd>↓</kbd> <kbd>←</kbd> <kbd>→</kbd> Preview another file
 - <kbd>Ctrl</kbd>+<kbd>Wheel</kbd> Zoom in/out (images & pdf)
 - <kbd>Wheel</kbd> Increase/decrease volume

## Supported file types, file manager intergation, etc.

See the [Wiki page](https://github.com/QL-Win/QuickLook/wiki)

## Translations

See the [Translation guide](https://github.com/QL-Win/QuickLook/wiki/Translations)

## Thanks to

 - Many [open-source projects](https://github.com/QL-Win/QuickLook/wiki/On-the-Shoulders-of-Giants) and their contributors
 - Our UI designers [@OiCkilL](https://twitter.com/OiCkilL) (“Fluent” user interface since v0.3) and [@QubitsDev](https://twitter.com/qubitsdev) (application icon since v0.3)
 - [Our contributers](https://github.com/QL-Win/QuickLook/graphs/contributors) who
     - teach QuickLook speaks *your* language
     - send pull requests, report bugs or give suggestions
 - ... and you 😊

## Licenses

![GPL-v3](https://www.gnu.org/graphics/gplv3-127x51.png)

This project references many other open-source projects. See [here](https://github.com/QL-Win/QuickLook/wiki/On-the-Shoulders-of-Giants) for the full list.

All source codes are licensed under [GPL-3.0](https://opensource.org/licenses/GPL-3.0).

If you want to make any modification on these source codes while keeping new codes not protected by GPL-3.0, please contact me for a sublicense instead.
