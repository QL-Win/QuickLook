$opensslExe = 'C:\Program Files\Git\usr\bin\openssl.exe'
$signExe = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x86\signtool.exe'
$timestampUrl = 'http://time.certum.pl/'
$version = git describe --always --tags "--abbrev=0" --exclude latest

if($version.Split('.').Length.Equals(3))
{
    $version += ".0"
}

Set-Location ../Build

if (-not (Test-Path sideload.key)) {
    .$opensslExe genrsa -out "sideload.key" 2048
}
if (-not (Test-Path sideload.pfx)) {
    .$opensslExe req -new -x509 -days 825 -key "sideload.key" -out "sideload.crt" -config "openssl-sign.cnf" -subj "/CN=CE36AF3D-FF94-43EB-9908-7EC8FD1D29FB"
    .$opensslExe pkcs12 -export -out "sideload.pfx" -inkey "sideload.key" -in "sideload.crt" -password pass:123456
}

.$signExe sign /fd sha256 /f "sideload.pfx" /p 123456 /td sha256 /tr $timestampUrl QuickLook-$version.appx
.$signExe verify /pa /v QuickLook-$version.appx
