$version = git describe --always --tags

Remove-Item ..\Build\QuickLook-$version.msi -ErrorAction SilentlyContinue
Remove-Item ..\Build\QuickLook-$version.zip -ErrorAction SilentlyContinue

Start-Sleep -s 1

Rename-Item ..\Build\QuickLook.msi QuickLook-$version.msi
Compress-Archive ..\Build\Package\* ..\Build\QuickLook-$version.zip