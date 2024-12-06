$version = git describe --always --tags --exclude latest

Start-Sleep -s 1

Write-Output "This file makes QuickLook portable." >> ..\Build\Package\portable.lock

Remove-Item ..\Build\QuickLook-$version.zip -ErrorAction SilentlyContinue
Compress-Archive ..\Build\Package\* ..\Build\QuickLook-$version.zip

Remove-Item ..\Build\Package\portable.lock