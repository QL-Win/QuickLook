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

using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace QuickLook.Common.NativeMethods;

public static class Kernel32
{
    [DllImport("kernel32.dll")]
    public static extern nint LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll")]
    public static extern int GetCurrentPackageFullName(ref uint packageFullNameLength,
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder packageFullName);

    [DllImport("kernel32.dll")]
    public static extern nint GetCurrentThreadId();

    [DllImport("kernel32.dll")]
    public static extern bool GetProductInfo(int dwOSMajorVersion, int dwOSMinorVersion, int dwSpMajorVersion,
        int dwSpMinorVersion, out uint pdwReturnedProductType);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern SafeFileHandle CreateFile(
        [MarshalAs(UnmanagedType.LPWStr)] string filename,
        [MarshalAs(UnmanagedType.U4)] FileAccess access,
        [MarshalAs(UnmanagedType.U4)] FileShare share,
        nint securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
        [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
        [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
        nint templateFile);
}
