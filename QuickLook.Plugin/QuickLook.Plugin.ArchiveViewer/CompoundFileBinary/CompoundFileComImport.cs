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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace QuickLook.Plugin.ArchiveViewer.CompoundFileBinary;

/// <summary>
/// A disposable wrapper for a COM <see cref="IStream"/> instance.
/// Provides convenience methods for reading, writing and querying stream metadata
/// while ensuring the underlying COM object is released when disposed.
/// </summary>
public class DisposableIStream : IDisposable
{
    /// <summary>
    /// The underlying COM stream object.
    /// </summary>
    public IStream Stream { get; private set; }

    /// <summary>
    /// Wrap an existing <see cref="IStream"/>.
    /// </summary>
    /// <param name="stream">The COM IStream to wrap.</param>
    public DisposableIStream(IStream stream)
    {
        Stream = stream;
    }

    /// <summary>
    /// Open a named stream from the given storage and wrap it.
    /// </summary>
    /// <param name="storage">Parent storage containing the stream.</param>
    /// <param name="name">Stream name within the storage.</param>
    /// <param name="mode">Access mode flags (STGM).</param>
    public DisposableIStream(IStorage storage, string name, STGM mode)
    {
        storage.OpenStream(name, IntPtr.Zero, mode, 0, out IStream stream);
        Stream = stream;
    }

    /// <summary>
    /// Read up to <paramref name="length"/> bytes from the stream into <paramref name="buffer"/>.
    /// Returns the number of bytes actually read.
    /// </summary>
    /// <param name="buffer">Destination buffer.</param>
    /// <param name="length">Maximum number of bytes to read.</param>
    /// <returns>Number of bytes read.</returns>
    public int Read(byte[] buffer, int length)
    {
        // Use unmanaged memory to receive the number of bytes read from IStream.Read.
        nint pcbRead = Marshal.AllocHGlobal(sizeof(int));
        Stream.Read(buffer, length, pcbRead);
        int bytesRead = Marshal.ReadInt32(pcbRead);
        Marshal.FreeHGlobal(pcbRead);
        return bytesRead;
    }

    /// <summary>
    /// Query the STATSTG information for this stream.
    /// </summary>
    /// <param name="statFlag">Flags controlling returned data (see <see cref="STATFLAG"/>).</param>
    /// <returns>A <see cref="STATSTG"/> describing the stream.</returns>
    public STATSTG Stat(int statFlag)
    {
        Stream.Stat(out STATSTG statstg, statFlag);
        return statstg;
    }

    /// <summary>
    /// Write <paramref name="length"/> bytes from <paramref name="buffer"/> into the stream.
    /// </summary>
    /// <param name="buffer">Source buffer containing data to write.</param>
    /// <param name="length">Number of bytes from buffer to write.</param>
    public void Write(byte[] buffer, int length)
    {
        nint pcbWritten = Marshal.AllocHGlobal(sizeof(int));
        Stream.Write(buffer, length, pcbWritten);
        Marshal.FreeHGlobal(pcbWritten);
    }

    /// <summary>
    /// Releases the wrapped COM <see cref="IStream"/> instance.
    /// </summary>
    public void Dispose()
    {
        if (Stream != null)
        {
            _ = Marshal.ReleaseComObject(Stream);
            Stream = null!;
        }
    }
}

/// <summary>
/// A disposable wrapper for a COM <see cref="IStorage"/> instance.
/// Provides helper methods to open nested storages/streams and enumerate children.
/// </summary>
public class DisposableIStorage : IDisposable
{
    /// <summary>
    /// Create a new structured storage file at <paramref name="filePath"/> using the provided <paramref name="mode"/>.
    /// This wraps <c>StgCreateStorageEx</c> and throws an exception on failure.
    /// </summary>
    /// <param name="filePath">Filesystem path for the new storage.</param>
    /// <param name="mode">STGM flags controlling creation mode.</param>
    /// <returns>A new <see cref="DisposableIStorage"/> wrapping the created storage.</returns>
    public static DisposableIStorage CreateStorage(string filePath, STGM mode)
    {
        // GUID for property set storage (V4); passed to StgCreateStorageEx.
        Guid propertySetStorageId = new("0000013A-0000-0000-C000-000000000046");

        STGOPTIONS options;
        options.usVersion = 1;
        options.reserved = 0;
        options.ulSectorSize = 4096;

        int hr = Ole32.StgCreateStorageEx(filePath, mode, STGFMT.STGFMT_DOCFILE, 0, ref options, IntPtr.Zero, ref propertySetStorageId, out IStorage storage);
        if (hr != HRESULT.S_OK)
        {
            Exception ex = Marshal.GetExceptionForHR(hr);
            throw new Exception("Error while creating file: " + (ex?.Message));
        }
        return new DisposableIStorage(storage);
    }

    /// <summary>
    /// The underlying COM IStorage instance.
    /// </summary>
    public IStorage Storage { get; private set; }

    private DisposableIStorage(IStorage storage)
    {
        Storage = storage;
    }

    /// <summary>
    /// Open an existing structured storage file and wrap it.
    /// This calls <c>StgOpenStorage</c> and throws an exception on failure.
    /// </summary>
    /// <param name="filePath">Path to the storage file to open.</param>
    /// <param name="mode">STGM flags controlling open mode.</param>
    /// <param name="excludeNames">Reserved parameter passed to native API for exclude names mask.</param>
    public DisposableIStorage(string filePath, STGM mode, nint excludeNames)
    {
        int hr = Ole32.StgOpenStorage(filePath, null, mode, excludeNames, 0, out IStorage storage);
        if (hr != HRESULT.S_OK)
        {
            Exception ex = Marshal.GetExceptionForHR(hr);
            throw new Exception("Error while opening file: " + (ex?.Message));
        }
        Storage = storage;
    }

    /// <summary>
    /// Open a nested storage (child directory-like storage) and return a new wrapper.
    /// </summary>
    /// <param name="name">Name of the nested storage.</param>
    /// <param name="priorityStorage">Optional priority storage parameter for native call.</param>
    /// <param name="mode">STGM access flags.</param>
    /// <param name="excludeNames">Reserved exclude names mask.</param>
    /// <returns>A <see cref="DisposableIStorage"/> wrapping the opened nested storage.</returns>
    public DisposableIStorage OpenStorage(string name, IStorage priorityStorage, STGM mode, nint excludeNames)
    {
        Storage.OpenStorage(name, priorityStorage, mode, excludeNames, 0, out IStorage subStorage);
        return new DisposableIStorage(subStorage);
    }

    /// <summary>
    /// Open a named stream from this storage and return a disposable wrapper for it.
    /// </summary>
    /// <param name="name">Name of the stream.</param>
    /// <param name="reserved1">Reserved pointer passed to native call.</param>
    /// <param name="mode">STGM access flags.</param>
    /// <returns>A <see cref="DisposableIStream"/> wrapping the opened stream.</returns>
    public DisposableIStream OpenStream(string name, nint reserved1, STGM mode)
    {
        Storage.OpenStream(name, reserved1, mode, 0, out IStream stream);
        return new DisposableIStream(stream);
    }

    /// <summary>
    /// Create a new stream within this storage and return a wrapper for writing.
    /// </summary>
    /// <param name="name">Name for the new stream.</param>
    /// <param name="mode">STGM flags controlling creation mode.</param>
    /// <returns>A <see cref="DisposableIStream"/> for the created stream.</returns>
    public DisposableIStream CreateStream(string name, STGM mode)
    {
        Storage.CreateStream(name, mode, 0, 0, out IStream stream);
        return new DisposableIStream(stream);
    }

    /// <summary>
    /// Enumerate child elements (streams and storages) of this storage.
    /// Each yielded <see cref="STATSTG"/> describes a single child element.
    /// </summary>
    /// <remarks>
    /// The caller should not assume ownership of the returned <see cref="STATSTG"/> structures; they are copies of the native data.
    /// </remarks>
    /// <returns>An enumerator yielding <see cref="STATSTG"/> entries.</returns>
    public IEnumerator<STATSTG> EnumElements()
    {
        Storage.EnumElements(0, IntPtr.Zero, 0, out IEnumSTATSTG enumStatStg);
        STATSTG[] statStg = new STATSTG[1];
        while (enumStatStg.Next(1, statStg, out uint fetched) == 0 && fetched > 0)
        {
            yield return statStg[0];
        }
        _ = Marshal.ReleaseComObject(enumStatStg);
    }

    /// <summary>
    /// Releases the wrapped COM <see cref="IStorage"/> instance.
    /// </summary>
    public void Dispose()
    {
        if (Storage != null)
        {
            _ = Marshal.ReleaseComObject(Storage);
            Storage = null!;
        }
    }
}

/// <summary>
/// Enumerates the STATSTG structures returned by <see cref="IStorage.EnumElements"/>.
/// </summary>
[ComImport]
[Guid("0000000d-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IEnumSTATSTG
{
    /// <summary>
    /// Retrieves a specified number of items in the enumeration sequence.
    /// </summary>
    /// <param name="requestedCount">The number of items to be retrieved.</param>
    /// <param name="elements">An array of STATSTG items.</param>
    /// <param name="fetchedCount">The number of items actually retrieved.</param>
    /// <returns>S_OK if the number of items supplied is celt; otherwise, S_FALSE.</returns>
    [PreserveSig]
    public uint Next(uint requestedCount, [MarshalAs(UnmanagedType.LPArray), Out] STATSTG[] elements, out uint fetchedCount);

    /// <summary>
    /// Skips a specified number of items in the enumeration sequence.
    /// </summary>
    /// <param name="count">The number of items to be skipped.</param>
    public void Skip(uint count);

    /// <summary>
    /// Resets the enumeration sequence to the beginning.
    /// </summary>
    public void Reset();

    /// <summary>
    /// Creates a new enumerator that contains the same enumeration state as the current one.
    /// </summary>
    /// <returns>A clone of the current enumerator.</returns>
    [return: MarshalAs(UnmanagedType.Interface)]
    public IEnumSTATSTG Clone();
}

/// <summary>
/// Provides methods for creating and managing the root storage object, child storage objects, and stream objects.
/// Represents the COM <c>IStorage</c> interface.
/// </summary>
[ComImport]
[Guid("0000000b-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IStorage
{
    /// <summary>
    /// Creates a new stream object in this storage object with the specified name and access mode.
    /// </summary>
    /// <param name="name">Name of the stream to create.</param>
    /// <param name="mode">STGM flags that specify access and creation options.</param>
    /// <param name="reserved1">Reserved; must be zero.</param>
    /// <param name="reserved2">Reserved; must be zero.</param>
    /// <param name="stream">Receives the created <see cref="IStream"/> instance.</param>
    public void CreateStream(string name, STGM mode, uint reserved1, uint reserved2, out IStream stream);

    /// <summary>
    /// Opens the specified stream object in this storage object and returns a pointer to the <see cref="IStream"/> interface.
    /// </summary>
    /// <param name="name">Name of the stream to open.</param>
    /// <param name="reserved1">Reserved pointer passed to native call (typically IntPtr.Zero).</param>
    /// <param name="mode">STGM flags that specify access mode.</param>
    /// <param name="reserved2">Reserved; must be zero.</param>
    /// <param name="stream">Receives the opened <see cref="IStream"/> instance.</param>
    public void OpenStream(string name, nint reserved1, STGM mode, uint reserved2, out IStream stream);

    /// <summary>
    /// Creates a new storage object (nested storage) with the specified name and access mode.
    /// </summary>
    /// <param name="name">Name of the nested storage to create.</param>
    /// <param name="mode">STGM flags that specify access and creation options.</param>
    /// <param name="reserved1">Reserved; must be zero.</param>
    /// <param name="reserved2">Reserved; must be zero.</param>
    /// <param name="storage">Receives the created <see cref="IStorage"/> instance.</param>
    public void CreateStorage(string name, STGM mode, uint reserved1, uint reserved2, out IStorage storage);

    /// <summary>
    /// Opens an existing nested storage object by name and returns a pointer to the <see cref="IStorage"/> interface.
    /// </summary>
    /// <param name="name">Name of the nested storage to open.</param>
    /// <param name="priorityStorage">Optional priority storage used by the native API.</param>
    /// <param name="mode">STGM flags that specify access mode.</param>
    /// <param name="excludeNames">Reserved exclude names mask.</param>
    /// <param name="reserved">Reserved; must be zero.</param>
    /// <param name="storage">Receives the opened <see cref="IStorage"/> instance.</param>
    public void OpenStorage(string name, IStorage priorityStorage, STGM mode, nint excludeNames, uint reserved, out IStorage storage);

    /// <summary>
    /// Copies the specified elements and interfaces from this storage object to another storage object.
    /// </summary>
    /// <param name="excludedInterfaceCount">Number of interface IDs in the excluded list.</param>
    /// <param name="excludedInterfaceIds">GUID identifying interfaces to exclude (native signature uses a pointer/array).</param>
    /// <param name="excludeNames">Reserved exclude names mask.</param>
    /// <param name="destStorage">Destination storage that receives the copied elements.</param>
    public void CopyTo(uint excludedInterfaceCount, Guid excludedInterfaceIds, nint excludeNames, IStorage destStorage);

    /// <summary>
    /// Moves or renames an element from this storage to a destination storage.
    /// </summary>
    /// <param name="name">The current name of the element to move.</param>
    /// <param name="destStorage">Destination storage to receive the element.</param>
    /// <param name="newName">New name for the element in the destination storage.</param>
    /// <param name="flags">Flags that control the operation.</param>
    public void MoveElementTo(string name, IStorage destStorage, string newName, uint flags);

    /// <summary>
    /// Commits changes made to this storage object to the underlying storage medium.
    /// </summary>
    /// <param name="commitFlags">Flags that control commit behavior.</param>
    public void Commit(uint commitFlags);

    /// <summary>
    /// Discards changes that have been made to this storage object since the last commit.
    /// </summary>
    public void Revert();

    /// <summary>
    /// Enumerates the elements contained in this storage object.
    /// </summary>
    /// <param name="reserved1">Reserved value passed to the native API.</param>
    /// <param name="reserved2">Reserved pointer passed to the native API.</param>
    /// <param name="reserved3">Reserved value passed to the native API.</param>
    /// <param name="enumStat">Receives an <see cref="IEnumSTATSTG"/> enumerator for the elements.</param>
    public void EnumElements(uint reserved1, nint reserved2, uint reserved3, out IEnumSTATSTG enumStat);

    /// <summary>
    /// Destroys the specified element (stream or storage) within this storage object.
    /// </summary>
    /// <param name="name">Name of the element to destroy.</param>
    public void DestroyElement(string name);

    /// <summary>
    /// Renames an existing element within this storage object.
    /// </summary>
    /// <param name="oldName">Current name of the element.</param>
    /// <param name="newName">New name for the element.</param>
    public void RenameElement(string oldName, string newName);

    /// <summary>
    /// Sets the creation, access and modification times for the specified element.
    /// </summary>
    /// <param name="name">Name of the element whose timestamps will be updated.</param>
    /// <param name="creationTime">Creation time to set.</param>
    /// <param name="accessTime">Last access time to set.</param>
    /// <param name="modificationTime">Last modification time to set.</param>
    public void SetElementTimes(string name, FILETIME creationTime, FILETIME accessTime, FILETIME modificationTime);

    /// <summary>
    /// Sets the class identifier (CLSID) for this storage object.
    /// </summary>
    /// <param name="clsid">CLSID to associate with the storage.</param>
    public void SetClass(Guid clsid);

    /// <summary>
    /// Sets state bits for this storage object.
    /// </summary>
    /// <param name="stateBits">State bits to set.</param>
    /// <param name="mask">Mask specifying which bits to change.</param>
    public void SetStateBits(uint stateBits, uint mask);

    /// <summary>
    /// Retrieves the STATSTG structure that contains statistical information about this storage object or an element.
    /// </summary>
    /// <param name="statStg">Receives the STATSTG structure.</param>
    /// <param name="statFlag">Specifies whether the name is returned and/or other options (see <see cref="STATFLAG"/>).</param>
    public void Stat(out STATSTG statStg, uint statFlag);
}

/// <summary>
/// The STGM constants are flags that indicate conditions for creating and deleting the object and access modes for the object.
/// These are passed to storage and stream creation/opening methods.
/// </summary>
[Flags]
public enum STGM : int
{
    DIRECT = 0x00000000,
    TRANSACTED = 0x00010000,
    SIMPLE = 0x08000000,
    READ = 0x00000000,
    WRITE = 0x00000001,
    READWRITE = 0x00000002,
    SHARE_DENY_NONE = 0x00000040,
    SHARE_DENY_READ = 0x00000030,
    SHARE_DENY_WRITE = 0x00000020,
    SHARE_EXCLUSIVE = 0x00000010,
    PRIORITY = 0x00040000,
    DELETEONRELEASE = 0x04000000,
    NOSCRATCH = 0x00100000,
    CREATE = 0x00001000,
    CONVERT = 0x00020000,
    FAILIFTHERE = 0x00000000,
    NOSNAPSHOT = 0x00200000,
    DIRECT_SWMR = 0x00400000,
}

/// <summary>
/// Specifies whether the STATSTG structure contains the name of the storage object.
/// </summary>
public enum STATFLAG : uint
{
    STATFLAG_DEFAULT = 0,
    STATFLAG_NONAME = 1,
    STATFLAG_NOOPEN = 2,
}

/// <summary>
/// The STGTY enumeration values specify the type of a storage object.
/// STGTY_STORAGE represents a nested storage (similar to a directory).
/// STGTY_STREAM represents a stream (similar to a file) inside a storage.
/// </summary>
public enum STGTY : int
{
    /// <summary>
    /// A nested storage object (treat as a directory when extracting).
    /// </summary>
    STGTY_STORAGE = 1,

    /// <summary>
    /// A stream object (treat as a file when extracting).
    /// </summary>
    STGTY_STREAM = 2,

    STGTY_LOCKBYTES = 3,
    STGTY_PROPERTY = 4,
}

/// <summary>
/// The STGFMT enumeration values specify the format of a storage object.
/// </summary>
public enum STGFMT : int
{
    STGFMT_STORAGE = 0,
    STGFMT_FILE = 3,
    STGFMT_ANY = 4,
    STGFMT_DOCFILE = 5,
}

[StructLayout(LayoutKind.Sequential)]
public struct STGOPTIONS
{
    public ushort usVersion;
    public ushort reserved;
    public uint ulSectorSize;
}

file static class Ole32
{
    /// <summary>
    /// Determines whether the given path is a structured storage file.
    /// </summary>
    [DllImport("ole32.dll")]
    public static extern int StgIsStorageFile([MarshalAs(UnmanagedType.LPWStr)] string filePath);

    /// <summary>
    /// Opens an existing compound file and returns an IStorage interface.
    /// This is the managed signature for the native StgOpenStorage function.
    /// </summary>
    [DllImport("ole32.dll")]
    public static extern int StgOpenStorage(
        [MarshalAs(UnmanagedType.LPWStr)] string filePath,
        IStorage priorityStorage,
        STGM mode,
        nint excludeNames,
        uint reserved,
        out IStorage openStorage);

    /// <summary>
    /// Creates a new structured storage file. Managed signature for StgCreateStorageEx.
    /// </summary>
    [DllImport("ole32.dll")]
    public static extern int StgCreateStorageEx(
        [MarshalAs(UnmanagedType.LPWStr)] string filePath,
        STGM mode,
        STGFMT format,
        uint attrs,
        ref STGOPTIONS options,
        nint securityDescriptor,
        ref Guid riid,
        out IStorage openObject);
}

file static class HRESULT
{
    /// <summary>
    /// Success HRESULT code.
    /// </summary>
    public const int S_OK = 0;
}
