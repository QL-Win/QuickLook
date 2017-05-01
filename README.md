# QuickLook

*This project is currently under heavy development. Always come back to see what's new.*

<img src="http://pooi.moe/QuickLook/sample.gif" width="400">

## Background
[Quick Look](https://en.wikipedia.org/wiki/Quick_Look) is among the a few features I miss from Mac OS X. It enables *very* quick preview of file by pressing <kbd>Space</kbd> key while highlighting it, without opening its associated application. Then I decide to add this feature to Windows by myself, which results this “QuickLook” project.

You may ask, why you write this when there several alternatives available on the Internet (e.g. [WinQuickLook](https://github.com/shibayan/WinQuickLook) and [Seer](https://github.com/ccseer/Seer))? The answer is that, they are all among those which no longer actively developed, lack of support of file types and plugins, or asking user for amounts of $$$.

## Features
Till now, QuickLook supports the preview of 

 - Images: e.g. `.png`, `.jpg`, `.bmp` and `.gif`
 - Compressed archives: `.zip`, `.rar`, `.7z` etc.
 - Pdf file
 - All kinds of text files (determined by file content)
 - Microsoft Word (`.doc`, `.docx`), Excel (`.xls`, `.xlsx`) and PowerPoint (`.ppt`, `.pptx`) files (requires MS Office installation)
 - Other files and folders will be shown in a information box

Hotkeys in preview window:

 - <kbd>Space</kbd> Show/Hide the preview window
 - <kbd>Ctrl+Wheel</kbd> Zoom in/out

## Development

The previewing ability can be extended by new plugins. Read the  [plugin interface](https://github.com/xupefei/QuickLook/blob/master/QuickLook/Plugin/IViewer.cs), [context object](https://github.com/xupefei/QuickLook/blob/master/QuickLook/Plugin/ContextObject.cs) for more information. [Pre-shipped plugins](https://github.com/xupefei/QuickLook/tree/master/QuickLook.Plugin) contains more detailed implementation.
Note that any plugin must be under the `QuickLook.Plugin` namespace, has the filename similar to `QuickLook.Plugin.YourPlugin.dll` and placed under `<Application>\Plugins\QuickLook.Plugin.YourPlugin\` subfolder.
