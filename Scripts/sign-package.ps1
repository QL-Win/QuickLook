if(-not (Test-Path env:CI))
{
    $signExe = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.16299.0\x86\signtool.exe'
    $files = gci '..\Build\Package\*' -Recurse -Include QuickLook*.exe,QuickLook*.dll | %{('"{0}"' -f $_.FullName)}
    $timestampUrl = 'http://time.certum.pl/'

    .$signExe sign /a /v /fd sha256 /t $timestampUrl $files
}