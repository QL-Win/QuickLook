$version = git describe --always --tags

Start-Sleep -s 1

Remove-Item ..\Build\QuickLook-$version.zip -ErrorAction SilentlyContinue
Compress-Archive ..\Build\Package\* ..\Build\QuickLook-$version.zip