$version = git describe --always --tags

Rename-Item ..\Build\QuickLook.msi QuickLook-$version.msi
Compress-Archive ..\Build\Package\* ..\Build\QuickLook-$version.zip