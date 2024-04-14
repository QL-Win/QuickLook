# How to build QuickLook
========================

## Env Setup

### Visual Studio Payload

Download [Visual Studio](https://visualstudio.microsoft.com/), At least VC++ / .net Desktop / Universal Windows payloads are required to build this project. 

### SDKs

- Windows 10/11 SDK
- .net framework 4.6.2 SDK 

You can choose from Visual Studio Installer.

### Others

- wix, tool to create installer,  `dotnet tool install wix -g`
- git (of course),  `winget install git.git`
- nuget (for CLI build purpose, if you use Visual Studio, this is not required),  `winget install Microsoft.NuGet`

## Build Steps

### 1. Fork and Clone

Fork `https://github.com/QL-Win/QuickLook`

```powershell
git clone https://git-repo-path.git  --recurse-submodules
```

### 2. Open *Developer PowerShell* From StartMenu.

Navigate to your source path.

```powershell
cd PATH_TO_YOUR_CLONED_PRJECT
```

### 3. Try to Build

Build in Visual Studio, or you can use CLI as well:

```powershell
nuget restore .
msbuild QuickLook.sln
```

### 4. Tips

1. If you encountered an error msg of ```Incorrect Version Number, should be major[.minor[.build[.revision]]]```, modify Scripts/update-version.ps1, change ```$tag``` to ```3.7.4``` for example, then build again.

2. Make sure these 3 files are valid references in Project QuickLook.

- "\QuickLook\windows.winmd"
- "\QuickLook\Windows.Foundation.FoundationContract.winmd"
- "\QuickLook\Windows.Foundation.UniversalApiContract.winmd"

3. You need to change windows SDK paths in /Scripts in order to run those scripts.
