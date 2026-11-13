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
using System.Runtime.InteropServices;

namespace QuickLook.Plugin.InfoPanel;

/// <summary>
///     WPF-free helpers that read metadata of an arbitrary shell item.
/// </summary>
/// <remarks>
///     The paths QuickLook receives from Explorer are not always Win32 file system paths.
///     Items on an MTP device (a phone connected via USB), items inside a zip folder or items of a
///     virtual shell folder are addressed by "shell parsing names" such as
///     <c>::{20D04FE0-3AEA-1069-A2D8-08002B30309D}\?\usb#vid_04e8&amp;pid_6860#...</c>.
///     System.IO APIs throw <see cref="ArgumentException" /> ("Illegal characters in path") for those,
///     so the shell property system is the only safe way to obtain their metadata.
///     Every method in this class is defensive: it never throws, failure is reported by the return value.
/// </remarks>
public static class ShellItemInfo
{
    private const string IID_IShellItem2 = "7E9FB0D3-919F-4307-AB2E-9B1860310C93";

    // PROPERTYKEY fmtid shared by the "System" property family (PKEY_Size, PKEY_DateModified, ...).
    private static readonly Guid SystemPropertyFormatId = new Guid("B725F130-47EF-101A-A5F1-02608C9EEBAC");

    private const uint PKEYItemTypeTextPid = 4;
    private const uint PKEYSizePid = 12;
    private const uint PKEYDateModifiedPid = 14;

    private const uint SFGAO_STREAM = 0x00400000;
    private const uint SFGAO_FOLDER = 0x20000000;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem2 shellItem);

    /// <summary>
    ///     Returns true when <paramref name="path" /> can be resolved to a shell item.
    ///     This includes ordinary file system paths and shell-only parsing names.
    /// </summary>
    public static bool IsShellItem(string path)
    {
        var item = CreateShellItem(path);

        if (item == null)
            return false;

        ReleaseComObject(item);
        return true;
    }

    /// <summary>
    ///     Gets the name Explorer shows for the item (SIGDN_NORMALDISPLAY), which is the only usable
    ///     name source for shell-only items where System.IO.Path.GetFileName would throw.
    /// </summary>
    public static bool TryGetDisplayName(string path, out string displayName)
    {
        displayName = null;

        var item = CreateShellItem(path);

        if (item == null)
            return false;

        var pszName = IntPtr.Zero;

        try
        {
            if (item.GetDisplayName(SIGDN.NORMALDISPLAY, out pszName) != 0 || pszName == IntPtr.Zero)
                return false;

            var name = Marshal.PtrToStringUni(pszName);

            if (string.IsNullOrEmpty(name))
                return false;

            displayName = name;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            if (pszName != IntPtr.Zero)
                Marshal.FreeCoTaskMem(pszName);

            ReleaseComObject(item);
        }
    }

    /// <summary>The display name, or null when it cannot be determined.</summary>
    public static string GetDisplayNameOrNull(string path)
    {
        return TryGetDisplayName(path, out var displayName) ? displayName : null;
    }

    /// <summary>
    ///     Gets the item size (PKEY_Size), which works both for file system items and for shell items
    ///     such as files on an MTP device. For folders the property may be 0 or missing.
    /// </summary>
    public static bool TryGetSize(string path, out long size)
    {
        size = 0L;

        var item = CreateShellItem(path);

        if (item == null)
            return false;

        try
        {
            var key = CreateSystemPropertyKey(PKEYSizePid);

            if (item.GetUInt64(ref key, out var value) != 0)
                return false;

            if (value > long.MaxValue)
                return false;

            size = (long)value;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            ReleaseComObject(item);
        }
    }

    /// <summary>
    ///     Gets the item modification time (PKEY_DateModified) as local time. This is the fallback for
    ///     shell-only items where System.IO.File.GetLastWriteTime throws.
    /// </summary>
    public static bool TryGetDateModified(string path, out DateTime modified)
    {
        modified = DateTime.MinValue;

        var item = CreateShellItem(path);

        if (item == null)
            return false;

        try
        {
            var key = CreateSystemPropertyKey(PKEYDateModifiedPid);

            if (item.GetFileTime(ref key, out var fileTime) != 0)
                return false;

            var value = ((long)fileTime.dwHighDateTime << 32) | fileTime.dwLowDateTime;

            if (value <= 0)
                return false;

            modified = DateTime.FromFileTimeUtc(value).ToLocalTime();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            ReleaseComObject(item);
        }
    }

    /// <summary>Gets the item type description Explorer shows (PKEY_ItemTypeText), e.g. "JPEG image".</summary>
    public static bool TryGetTypeText(string path, out string typeText)
    {
        typeText = null;

        var item = CreateShellItem(path);

        if (item == null)
            return false;

        var pszType = IntPtr.Zero;

        try
        {
            var key = CreateSystemPropertyKey(PKEYItemTypeTextPid);

            if (item.GetString(ref key, out pszType) != 0 || pszType == IntPtr.Zero)
                return false;

            var text = Marshal.PtrToStringUni(pszType);

            if (string.IsNullOrEmpty(text))
                return false;

            typeText = text;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            if (pszType != IntPtr.Zero)
                Marshal.FreeCoTaskMem(pszType);

            ReleaseComObject(item);
        }
    }

    /// <summary>
    ///     Returns true when the item is a container (folder) rather than a file/stream.
    ///     Shell folders are not necessarily directories on a device, hence the SFGAO check.
    /// </summary>
    public static bool TryGetIsFolder(string path, out bool isFolder)
    {
        isFolder = false;

        var item = CreateShellItem(path);

        if (item == null)
            return false;

        try
        {
            if (item.GetAttributes(SFGAO_FOLDER | SFGAO_STREAM, out var attributes) != 0)
                return false;

            isFolder = (attributes & SFGAO_FOLDER) != 0 && (attributes & SFGAO_STREAM) == 0;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            ReleaseComObject(item);
        }
    }

    private static PROPERTYKEY CreateSystemPropertyKey(uint pid)
    {
        return new PROPERTYKEY(SystemPropertyFormatId, pid);
    }

    private static IShellItem2 CreateShellItem(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var riid = new Guid(IID_IShellItem2);
            var hr = SHCreateItemFromParsingName(path, IntPtr.Zero, ref riid, out var shellItem);

            if (hr < 0 || shellItem == null)
                return null;

            return shellItem;
        }
        catch (Exception)
        {
            // Badly formed parsing names make the shell API throw as well, not just fail.
            return null;
        }
    }

    private static void ReleaseComObject(object comObject)
    {
        if (comObject == null)
            return;

        try
        {
            if (Marshal.IsComObject(comObject))
                Marshal.ReleaseComObject(comObject);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private enum SIGDN : uint
    {
        NORMALDISPLAY = 0
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;

        public PROPERTYKEY(Guid formatId, uint propertyId)
        {
            fmtid = formatId;
            pid = propertyId;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    // Minimal IShellItem2 vtable (IUnknown + IShellItem + IShellItem2 methods in vtable order) up to
    // the methods used above. The interface is declared with the IShellItem2 IID, so SHCreateItemFromParsingName
    // returns the object behind it directly and no separate IShellItem query is required.
    [ComImport]
    [Guid(IID_IShellItem2)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem2
    {
        [PreserveSig]
        int BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

        [PreserveSig]
        int GetParent(out IntPtr ppsi);

        [PreserveSig]
        int GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);

        [PreserveSig]
        int GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

        [PreserveSig]
        int Compare(IntPtr psi, uint hint, out int piOrder);

        [PreserveSig]
        int GetPropertyStore(uint flags, ref Guid riid, out IntPtr ppv);

        [PreserveSig]
        int GetPropertyStoreForKeys(IntPtr rgKeys, uint cKeys, uint flags, ref Guid riid, out IntPtr ppv);

        [PreserveSig]
        int GetPropertyDescriptionList(ref PROPERTYKEY keyType, ref Guid riid, out IntPtr ppv);

        [PreserveSig]
        int Update(IntPtr pbc);

        [PreserveSig]
        int GetProperty(ref PROPERTYKEY key, IntPtr ppropvar);

        [PreserveSig]
        int GetClsid(ref PROPERTYKEY key, out Guid pclsid);

        [PreserveSig]
        int GetFileTime(ref PROPERTYKEY key, out FILETIME pft);

        [PreserveSig]
        int GetInt32(ref PROPERTYKEY key, out int pi);

        [PreserveSig]
        int GetString(ref PROPERTYKEY key, out IntPtr ppsz);

        [PreserveSig]
        int GetUInt32(ref PROPERTYKEY key, out uint pui);

        [PreserveSig]
        int GetUInt64(ref PROPERTYKEY key, out ulong pull);

        [PreserveSig]
        int GetBool(ref PROPERTYKEY key, out int pf);

        [PreserveSig]
        int GetIcon(ref PROPERTYKEY key, uint flags, out IntPtr phicon);
    }
}
