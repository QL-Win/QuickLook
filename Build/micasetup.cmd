cd /d %~dp0
@REM 7z a Package.7z .\Package\* -t7z -mx=5 -mf=BCJ2 -r -y
makemica micasetup.json
@pause
