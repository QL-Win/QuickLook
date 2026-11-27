function ThrowAndPause([string]$message) {
    Write-Host $message -ForegroundColor Red
    Write-Host "Press any key to exit..."
    [void][System.Console]::ReadKey($true)
    throw $message
}

$version = git describe --always --tags --exclude latest
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($version)) {
    ThrowAndPause "Failed to read the version from git describe."
}

# NuGet global packages folder
$globalPackages = (dotnet nuget locals global-packages --list) -split ":\s*", 2 | Select-Object -Last 1
$globalPackages = "$globalPackages".Trim()

# 7z.exe from MicaSetup.Tools
$sevenZip = Join-Path $globalPackages "micasetup.tools\2.6.1\build\bin\7z.exe"

# makemica.exe from MicaSetup.Tools
$exePath = Join-Path $globalPackages "micasetup.tools\2.6.1\build\makemica.exe"

if (-not (Test-Path $sevenZip)) {
    ThrowAndPause "7z.exe was not found: $sevenZip"
}
if (-not (Test-Path $exePath)) {
    ThrowAndPause "makemica.exe was not found: $exePath"
}

# =========================
# Pack the application
# =========================
Write-Host "Packing application"
Write-Host ("Packing started {0:yyyy.MM.dd HH:mm:ss}" -f (Get-Date))

Set-Location (Join-Path $PSScriptRoot "..\Build")

if (-not (Test-Path ".\Package")) {
    ThrowAndPause "Build\Package was not found. Build the MSI first."
}

Write-Host "Removing old 7z package"
Remove-Item ".\Package.7z" -Force -ErrorAction SilentlyContinue
Remove-Item ".\Package\QuickLook.WoW64HookHelper.exe" -Force -ErrorAction SilentlyContinue

Write-Host "Creating 7z package"
& $sevenZip a ".\Package.7z" ".\Package\*" -t7z -mx=9 -ms=on -m0=lzma2 -mf=BCJ2 -r -y

if ($LASTEXITCODE -ne 0) {
    ThrowAndPause "7z packaging failed."
}

# =========================
# Build the installer
# =========================
Write-Host "Building installer"

Write-Host "Removing old installer"
Remove-Item ".\QuickLook.exe" -Force -ErrorAction SilentlyContinue
Remove-Item ".\QuickLook-$version.exe" -Force -ErrorAction SilentlyContinue

# makemica reads micasetup.json in the current directory
& $exePath "micasetup.json"

if ($LASTEXITCODE -ne 0) {
    ThrowAndPause "Installer build failed."
}

if (-not (Test-Path ".\QuickLook.exe")) {
    ThrowAndPause "Installer was not created: QuickLook.exe"
}

Write-Host "Renaming installer"
Rename-Item ".\QuickLook.exe" "QuickLook-$version.exe"

Write-Host "Removing temporary 7z package"
Remove-Item ".\Package.7z" -Force -ErrorAction SilentlyContinue

Write-Host "Installer created: QuickLook-$version.exe"
