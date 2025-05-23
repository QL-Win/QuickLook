# Please build the msi installation package in advance
powershell -file "pack-7z.ps1"
powershell -file "pack-setup.ps1"
powershell -file "pack-appx.ps1"
powershell -file "sign-appx.ps1"
Pause
