$version = git describe --always --tags "--abbrev=0" --exclude latest

if($version.Split('.').Length.Equals(3))
{
    $version += ".0"
}

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
    Copy-item ..\Build\AppxManifest.dev.xml ..\Build\Appx\AppxManifest.xml

    # set version to git version
    Update-Version("..\Build\Appx\AppxManifest.xml")

    # generate resources
    $priExe = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.17763.0\x86\makepri.exe'
    .$priExe new /pr ..\Build\Appx /cf ..\Build\priconfig.xml /of ..\Build\Appx\resources.pri
    
    # packing
    $packExe = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.17763.0\x86\makeappx.exe'
    $folder = '..\Build\Appx\'

    .$packExe pack /l /o /d ..\Build\Appx /p ..\Build\QuickLook-$version.appx
}
