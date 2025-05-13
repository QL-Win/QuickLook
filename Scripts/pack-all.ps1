# Please build the msi installation package in advance
powershell -file "$(SolutionDir)Scripts\pack-7z.ps1"
powershell -file "$(SolutionDir)Scripts\pack-setup.ps1"
powershell -file "$(SolutionDir)Scripts\pack-appx.ps1"
powershell -file "$(SolutionDir)Scripts\sign-appx.ps1"
Pause
