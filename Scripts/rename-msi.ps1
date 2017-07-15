$version = git describe --always --tags

Remove-Item ..\Build\QuickLook-$version.msi -ErrorAction SilentlyContinue
Rename-Item ..\Build\QuickLook.msi QuickLook-$version.msi