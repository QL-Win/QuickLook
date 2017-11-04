if(-not (Test-Path env:CI))
{
    $signExe = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.16299.0\x86\signtool.exe'
    $file = '..\Build\QuickLook.msi'
    $timestampUrl = 'http://time.certum.pl/'

    .$signExe sign /a /v /fd sha256 /t $timestampUrl $file
}