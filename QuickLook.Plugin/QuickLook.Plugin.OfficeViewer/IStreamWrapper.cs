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

namespace QuickLook.Plugin.OfficeViewer;

/// <summary>
/// Wraps a managed <see cref="Stream"/> as a COM IStream so that preview handlers
/// implementing <see cref="IInitializeWithStream"/> can read file content without
/// requiring a physical file path.
/// </summary>
internal sealed class IStreamWrapper : IStream
{
    private readonly Stream _stream;

    internal IStreamWrapper(Stream stream) => _stream = stream;

    public void Read(byte[] pv, int cb, IntPtr pcbRead)
    {
        int read = _stream.Read(pv, 0, cb);
        if (pcbRead != IntPtr.Zero)
            Marshal.WriteInt32(pcbRead, read);
    }

    public void Write(byte[] pv, int cb, IntPtr pcbWritten)
    {
        _stream.Write(pv, 0, cb);
        if (pcbWritten != IntPtr.Zero)
            Marshal.WriteInt32(pcbWritten, cb);
    }

    public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
    {
        long pos = _stream.Seek(dlibMove, (SeekOrigin)dwOrigin);
        if (plibNewPosition != IntPtr.Zero)
            Marshal.WriteInt64(plibNewPosition, pos);
    }

    public void SetSize(long libNewSize) => _stream.SetLength(libNewSize);

    public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten) =>
        throw new NotSupportedException();

    public void Commit(int grfCommitFlags) => throw new NotSupportedException();

    public void Revert() => throw new NotSupportedException();

    public void LockRegion(long libOffset, long cb, int dwLockType) =>
        throw new NotSupportedException();

    public void UnlockRegion(long libOffset, long cb, int dwLockType) =>
        throw new NotSupportedException();

    public void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag)
    {
        pstatstg = new System.Runtime.InteropServices.ComTypes.STATSTG
        {
            cbSize = _stream.Length,
        };
    }

    public void Clone(out IStream ppstm) => throw new NotSupportedException();
}
