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

using FreeTypeSharp;
using System;
using System.IO;
using System.Runtime.InteropServices;
using static FreeTypeSharp.FT;

namespace QuickLook.Plugin.FontViewer;

internal unsafe static class FreeTypeApi
{
    static FreeTypeApi()
    {
        FreeTypeDllMap.LoadNativeLibrary();
    }

    public static string GetFontFamilyName(string path)
    {
        if (!File.Exists(path)) return null;

        FT_LibraryRec_* lib;
        FT_FaceRec_* face;
        FT_Error error = FT_Init_FreeType(&lib);

        error = FT_New_Face(lib, (byte*)Marshal.StringToHGlobalAnsi(path), IntPtr.Zero, &face);

        if (error == FT_Error.FT_Err_Ok)
        {
            var familyName = Marshal.PtrToStringAnsi((nint)face->family_name);
            return familyName;
        }

        return null;
    }
}
