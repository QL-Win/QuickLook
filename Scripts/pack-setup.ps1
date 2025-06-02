$version = git describe --always --tags --exclude latest

Set-Location ../Build

Remove-Item .\Package.7z -ErrorAction SilentlyContinue
Remove-Item -Recurse .\Package\QuickLook.WoW64HookHelper.exe -ErrorAction SilentlyContinue
7z a Package.7z .\Package\* -t7z -mx=9 -ms=on -m0=lzma2 -mf=BCJ2 -r -y
makemica micasetup.json

Rename-Item .\QuickLook.exe QuickLook-$version.exe
Remove-Item .\Package.7z -ErrorAction SilentlyContinue