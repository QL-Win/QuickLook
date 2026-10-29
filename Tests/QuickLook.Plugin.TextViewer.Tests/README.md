Run the formatter and localization regression tests on Windows:

```powershell
dotnet test Tests/QuickLook.Plugin.TextViewer.Tests/QuickLook.Plugin.TextViewer.Tests.csproj
```

The tests target .NET Framework 4.6.2 and link the production formatter source, so they do not require the native QuickLook components or a desktop session. They verify JSON syntax, indentation, exact preservation of number and string tokens, duplicate keys, long compact input, formatting limits, and localization coverage. The preview menu and error dialog still require an interactive UI check.
