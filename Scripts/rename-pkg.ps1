$version = git describe --always --tags

Remove-Item ..\Build\QuickLook-$version.msi -ErrorAction SilentlyContinue
Remove-Item ..\Build\QuickLook-$version.zip -ErrorAction SilentlyContinue

Rename-Item ..\Build\QuickLook.msi QuickLook-$version.msi
Compress-Archive ..\Build\Package\* ..\Build\QuickLook-$version.zip