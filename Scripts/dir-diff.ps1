param(
    [Parameter(Mandatory=$true)]
    [string]$DirA,

    [Parameter(Mandatory=$true)]
    [string]$DirB,

    [switch]$Full
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Get-Map($root) {

    $root = (Resolve-Path $root).Path.TrimEnd('\')

    $map = @{}

    Get-ChildItem $root -Recurse -File | ForEach-Object {

        $rel = $_.FullName.Substring($root.Length).TrimStart('\')
        $map[$rel] = $_.Length
    }

    return $map
}

function Format-Size([long]$bytes) {

    if ($bytes -ge 1GB) { return "{0:N2} GB" -f ($bytes / 1GB) }
    if ($bytes -ge 1MB) { return "{0:N2} MB" -f ($bytes / 1MB) }
    if ($bytes -ge 1KB) { return "{0:N2} KB" -f ($bytes / 1KB) }
    return "$bytes B"
}

$mapA = Get-Map $DirA
$mapB = Get-Map $DirB

$allKeys = ($mapA.Keys + $mapB.Keys) | Sort-Object -Unique

foreach ($k in $allKeys) {

    $aHas = $mapA.ContainsKey($k)
    $bHas = $mapB.ContainsKey($k)

    $sizeA = if ($aHas) { $mapA[$k] } else { 0 }
    $sizeB = if ($bHas) { $mapB[$k] } else { 0 }

    $diff = $sizeB - $sizeA

    $aStr = Format-Size $sizeA
    $bStr = Format-Size $sizeB
    $diffStr = Format-Size ([math]::Abs($diff))

    if (-not $Full -and $diff -eq 0 -and $aHas -and $bHas) {
        continue
    }

    if (-not $aHas) {

        Write-Host "[+] $k ($aStr → $bStr, +$diffStr)" -ForegroundColor Green
    }
    elseif (-not $bHas) {

        Write-Host "[-] $k ($aStr → $bStr, -$diffStr)" -ForegroundColor Red
    }
    elseif ($diff -eq 0) {

        Write-Host "[=] $k ($aStr → $bStr, 0 B)" -ForegroundColor DarkGray
    }
    else {

        $color = if ($diff -gt 0) { "Yellow" } else { "Cyan" }
        $sign = if ($diff -gt 0) { "+" } else { "-" }

        Write-Host "[~] $k ($aStr → $bStr, $sign$diffStr)" -ForegroundColor $color
    }
}
