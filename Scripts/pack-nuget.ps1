Write-Host @"
███╗   ██╗██╗   ██╗ ██████╗ ███████╗████████╗
████╗  ██║██║   ██║██╔════╝ ██╔════╝╚══██╔══╝
██╔██╗ ██║██║   ██║██║  ███╗█████╗     ██║   
██║╚██╗██║██║   ██║██║   ██║██╔══╝     ██║   
██║ ╚████║╚██████╔╝╚██████╔╝███████╗   ██║   
╚═╝  ╚═══╝ ╚═════╝  ╚═════╝ ╚══════╝   ╚═╝   
"@

$revision = git describe --always --tags --exclude latest

if ($revision -match '^(\d+\.\d+\.\d+)(?:-(\d+)(?:-g[0-9a-f]+)?)?$') {
    $baseVersion = $matches[1]
    $commitCount = $matches[2]
    if ($commitCount) {
        $revision = "$baseVersion-preview$commitCount"
    } else {
        $revision = $baseVersion
    }
} else {
    throw "Unsupported git describe output: '$revision'. Expected 'x.y.z' or 'x.y.z-N-g<sha>'."
}

Write-Host "NuGet Package Version: $revision"

Set-Location ..\ # Move to the root of the project

powershell -ExecutionPolicy Bypass -File "Scripts\update-version.ps1"
dotnet pack -c Release -p:PackageVersion=$revision -o .\Build -p:PreBuildEvent="" QuickLook.Common\QuickLook.Common.csproj
dotnet pack -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:PackageVersion=$revision -o .\Build -p:PreBuildEvent=""  QuickLook.Common\QuickLook.Common.csproj

# Write-Host "`nPress any key to exit..."
# [void][System.Console]::ReadKey($true)
