// Copyright © 2024 QL-Win Contributors
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace QuickLook.Plugin.FontViewer;

/// <summary>
/// To implement a similar architecture detection logic in .NET Framework
/// https://github.com/ryancheung/FreeTypeSharp/blob/main/FreeTypeSharp/FT.DllMap.cs
/// </summary>
internal static class FreeTypeDllMap
{
    public static void LoadNativeLibrary()
    {
        _ = ImportResolver();
    }

    private static nint ImportResolver()
    {
        string actualLibraryName = "freetype.dll";
        string rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string arch = (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            ? "arm64"
            : (Environment.Is64BitProcess ? "x64" : "x86");

        var searchPaths = new[]
        {
            // This is where native libraries in our nupkg should end up
            Path.Combine(rootDirectory, "runtimes", "win-" + arch, "native"),
            Path.Combine(rootDirectory, "runtimes", "win-" + arch),
            Path.Combine(rootDirectory, "win-" + arch),
            Path.Combine(rootDirectory, arch),
            Path.Combine(rootDirectory)
        };

        foreach (var searchPath in searchPaths)
        {
            SetDllDirectory(searchPath);
            nint handle = LoadLibrary(Path.Combine(searchPath, actualLibraryName));

            if (handle != IntPtr.Zero)
                return handle;
        }

        return IntPtr.Zero;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern nint LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern bool SetDllDirectory(string lpPathName);
}
