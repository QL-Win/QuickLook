# ArchiveViewer regression runner

This dependency-free console runner targets net462 and must run on Windows.
It creates WPF controls on an STA thread without showing windows, injecting
desktop input, or automating external applications. Failures produce exit code 1.

From the repository root, with a .NET 10 SDK and .NET Framework 4.6.2 reference
assemblies (the SDK can restore the reference-assemblies package):

```powershell
$solutionDir = (Get-Location).Path + '\'
dotnet run --project ./Tests/ArchiveViewer.Regression/ArchiveViewer.Regression.csproj --configuration Release -p:Platform=AnyCPU -p:SolutionDir=$solutionDir
```

Use a full Git checkout with version tags so the existing Common project
pre-build event can generate GitVersion.cs. For shallow/offline checkouts,
prepare the ignored GitVersion.cs and cached dependencies first, then pass
`-p:PreBuildEvent=` to the command. The runner references the real plugin and
Common projects, so it builds the tested code rather than relying on stale DLLs.

After building, an optional local ZIP can be exercised by passing its path as
an argument to `bin/Release/net462/ArchiveViewer.Regression.exe`. The runner
reads its archive metadata locally and reports counts only; do not commit or
upload private archive fixtures.

Coverage includes focusability, routed tree-navigation keys, selection after
collection Reset, nested expansion restoration, RTL directions, full filename
tooltips, UI Automation names and ExpandCollapse provider behavior, a 10,000-row
expansion with bounded realized containers, narrow-window horizontal scrolling,
header alignment, lifecycle behavior and real empty/file-only/multi-root/nested
ZIP parsing. The ZIP fixtures are generated in a temporary directory and removed
after each test. No new testing packages are required.

These are control-level tests, not a substitute for manual keyboard focus,
screen-reader, high-DPI/theme or live desktop performance testing. They do not
establish a particular latency or peak memory guarantee, or validate RAR/7z.
