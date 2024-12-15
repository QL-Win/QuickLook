Set-Location ../Build

Remove-Item .\QuickLook.7z -ErrorAction SilentlyContinue
7z a QuickLook.7z .\Package\* -t7z -mx=5 -mf=BCJ2 -r -y
makemica micasetup.json

Write-Output "This file makes QuickLook portable." >> .\Package\portable.lock
7z a QuickLook.7z .\Package\portable.lock -t7z -mx=5 -mf=BCJ2 -r -y

$version = git describe --always --tags --exclude latest
Remove-Item .\QuickLook-$version.7z -ErrorAction SilentlyContinue
Rename-Item .\QuickLook.7z QuickLook-$version.7z
Remove-Item .\QuickLook-$version.exe -ErrorAction SilentlyContinue
Rename-Item .\QuickLook.exe QuickLook-$version.exe

pause
