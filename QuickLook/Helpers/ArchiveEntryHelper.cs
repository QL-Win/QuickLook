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

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;

namespace QuickLook.Helpers;

/// <summary>
/// Previews files that live inside an archive the shell opens as a folder (zip, and on newer Windows
/// 7z/rar...). Explorer hands those over as "C:\dir\file.zip\inner\name.txt", which no plugin can read,
/// so the entry is copied out through the shell to a temporary file first.
/// </summary>
internal static class ArchiveEntryHelper
{
    private static readonly string TempRoot = Path.Combine(Path.GetTempPath(), "QuickLook", "ArchiveEntries");

    /// <summary>
    /// Returns a temporary copy of the archive entry at <paramref name="path"/>, or null when the path
    /// is not a file inside an archive.
    /// </summary>
    public static string TryExtract(string path)
    {
        try
        {
            var archive = FindArchive(path);
            if (archive == null)
                return null;

            var item = FindEntry(archive, path.Substring(archive.Length + 1));
            if (item == null)
                return null;

            item.GetAttributes(SFGAO_FOLDER, out var attributes);
            if ((attributes & SFGAO_FOLDER) != 0)
                return null;

            // one folder per archive entry and archive version, so the same entry isn't copied twice
            var info = new FileInfo(archive);
            var dir = Path.Combine(TempRoot, Hash($"{path}|{info.Length}|{info.LastWriteTimeUtc.Ticks}"));
            var target = Path.Combine(dir, Path.GetFileName(path));
            if (File.Exists(target))
                return target;

            CleanupOldEntries();
            Directory.CreateDirectory(dir);

            var bhidStream = BHID_Stream;
            var iidStream = typeof(IStream).GUID;
            item.BindToHandler(IntPtr.Zero, ref bhidStream, ref iidStream, out var obj);
            var stream = (IStream)obj;
            try
            {
                CopyToFile(stream, target);
            }
            finally
            {
                Marshal.ReleaseComObject(stream);
            }

            return target;
        }
        catch (Exception e)
        {
            Common.Helpers.ProcessHelper.WriteLog(e.ToString());
            return null;
        }
    }

    /// <summary>
    /// True when <paramref name="path"/> is a folder inside an archive, e.g. "C:\dir\file.zip\docs".
    /// </summary>
    public static bool IsArchiveFolder(string path)
    {
        try
        {
            var archive = FindArchive(path);
            if (archive == null)
                return false;

            var item = FindEntry(archive, path.Substring(archive.Length + 1));
            if (item == null)
                return false;

            item.GetAttributes(SFGAO_FOLDER, out var attributes);
            return (attributes & SFGAO_FOLDER) != 0;
        }
        catch (Exception e)
        {
            Common.Helpers.ProcessHelper.WriteLog(e.ToString());
            return false;
        }
    }

    /// <summary>
    /// The nearest ancestor of a nonexistent path that is an existing file, e.g. "C:\dir\file.zip".
    /// </summary>
    private static string FindArchive(string path)
    {
        if (string.IsNullOrEmpty(path) || path.StartsWith("::"))
            return null;

        for (var parent = Path.GetDirectoryName(path); !string.IsNullOrEmpty(parent); parent = Path.GetDirectoryName(parent))
        {
            if (File.Exists(parent))
                return parent;
            if (Directory.Exists(parent))
                return null;
        }

        return null;
    }

    /// <summary>
    /// Walks the archive folder name by name: parsing a full path through a zip folder is unreliable.
    /// </summary>
    private static IShellItem FindEntry(string archive, string relativePath)
    {
        var iidShellItem = typeof(IShellItem).GUID;
        SHCreateItemFromParsingName(archive, IntPtr.Zero, ref iidShellItem, out var current);

        foreach (var name in relativePath.Split(['\\'], StringSplitOptions.RemoveEmptyEntries))
        {
            current = FindChild(current, name);
            if (current == null)
                return null;
        }

        return current;
    }

    private static IShellItem FindChild(IShellItem folder, string name)
    {
        var bhidEnum = BHID_EnumItems;
        var iidEnum = typeof(IEnumShellItems).GUID;
        folder.BindToHandler(IntPtr.Zero, ref bhidEnum, ref iidEnum, out var obj);
        var items = (IEnumShellItems)obj;

        var fetched = new IShellItem[1];
        while (items.Next(1, fetched, out var count) == 0 && count == 1)
        {
            var child = fetched[0];
            if (NameEquals(child, SIGDN.PARENTRELATIVEPARSING, name) || NameEquals(child, SIGDN.NORMALDISPLAY, name))
                return child;
        }

        return null;
    }

    private static bool NameEquals(IShellItem item, SIGDN sigdn, string name)
    {
        if (item.GetDisplayName(sigdn, out var ptr) != 0 || ptr == IntPtr.Zero)
            return false;

        var display = Marshal.PtrToStringUni(ptr);
        Marshal.FreeCoTaskMem(ptr);
        return string.Equals(display, name, StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyToFile(IStream stream, string target)
    {
        var buffer = new byte[81920];
        var readPtr = Marshal.AllocCoTaskMem(sizeof(int));
        try
        {
            using var output = new FileStream(target + ".part", FileMode.Create, FileAccess.Write, FileShare.None);
            while (true)
            {
                stream.Read(buffer, buffer.Length, readPtr);
                var read = Marshal.ReadInt32(readPtr);
                if (read <= 0)
                    break;
                output.Write(buffer, 0, read);
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(readPtr);
        }

        File.Move(target + ".part", target);
    }

    /// <summary>
    /// Removes copies made for earlier previews. Files still open (e.g. in a pinned window) are kept.
    /// </summary>
    private static void CleanupOldEntries()
    {
        if (!Directory.Exists(TempRoot))
            return;

        foreach (var dir in Directory.GetDirectories(TempRoot))
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
                // still in use
            }
        }
    }

    private static string Hash(string text)
    {
        using var sha = SHA1.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToLowerInvariant()));
        return BitConverter.ToString(bytes, 0, 8).Replace("-", string.Empty);
    }

    private const uint SFGAO_FOLDER = 0x20000000;
    private static readonly Guid BHID_Stream = new("1CEBB3AB-7C10-499A-A417-92CA16C4CB83");
    private static readonly Guid BHID_EnumItems = new("94F60519-2850-4924-AA5A-D15E84868039");

    private enum SIGDN : uint
    {
        NORMALDISPLAY = 0x00000000,
        PARENTRELATIVEPARSING = 0x80018001,
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(string path, IntPtr pbc, ref Guid riid, out IShellItem item);

    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);

        void GetParent(out IShellItem ppsi);

        [PreserveSig]
        int GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);

        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    [ComImport, Guid("70629033-E363-4A28-A567-0DB78006E6D7"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IEnumShellItems
    {
        [PreserveSig]
        int Next(uint celt, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface, SizeParamIndex = 0)] IShellItem[] rgelt, out uint pceltFetched);

        void Skip(uint celt);

        void Reset();

        void Clone(out IEnumShellItems ppenum);
    }
}
