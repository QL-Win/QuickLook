$version = git describe --always --tags "--abbrev=0"

function Update-Version($path)
{
    $xml = [xml](Get-Content $path)
    $xml.Package.Identity.Version="$version"
    $xml.Save("$path")
}

if(-not (Test-Path env:CI))
{
    # prapare folders
    Remove-Item -Recurse ..\Build\Appx -ErrorAction SilentlyContinue
    Copy-Item -Recurse ..\Build\Package ..\Build\Appx\Package
    Copy-Item -Recurse ..\Build\Assets ..\Build\Appx\Assets
    Copy-item ..\Build\AppxManifest.xml ..\Build\Appx

    # set version to git version
    Update-Version("..\Build\Appx\AppxManifest.xml")
    
    # packing
    $packExe = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.15063.0\x86\makeappx.exe'
    $folder = '..\Build\Appx\'

    .$packExe pack /o /d ..\Build\Appx /p ..\Build\QuickLook-$version.appx
}
