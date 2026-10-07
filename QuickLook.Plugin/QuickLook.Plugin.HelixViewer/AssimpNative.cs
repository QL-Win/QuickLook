// Copyright © 2017-2026 QL-Win Contributors
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

using Assimp.Unmanaged;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace QuickLook.Plugin.HelixViewer;

/// <summary>
/// AssimpNet resolves native DLLs from AppBaseDirectory (QuickLook.exe),
/// not the plugin folder where runtimes/win-*/native/assimp.dll is packaged.
/// Probe the plugin directory first. See #1741 #2016.
/// </summary>
internal static class AssimpNative
{
    public static void Load()
    {
        var library = AssimpLibrary.Instance;
        if (library.IsLibraryLoaded)
            return;

        string rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string arch = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? "arm64"
            : (Environment.Is64BitProcess ? "x64" : "x86");

        string[] probingPaths =
        [
            Path.Combine(rootDirectory, "runtimes", "win-" + arch, "native"),
            rootDirectory,
        ];

        foreach (var probingPath in probingPaths)
        {
            string dllPath = Path.Combine(probingPath, "assimp.dll");
            if (!File.Exists(dllPath))
                continue;

            library.LoadLibrary(dllPath);
            return;
        }

        library.Resolver.SetProbingPaths(probingPaths);
        library.LoadLibrary();
    }
}
