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

using System.Runtime.InteropServices;

namespace QuickLook.Plugin.OfficeViewer;

/// <summary>
/// COM interface for preview handlers that accept initialization via an IShellItem.
/// Used as a fallback when a handler does not implement IInitializeWithStream.
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("7f73be3f-fb79-493c-a6c7-7ee14e245841")]
internal interface IInitializeWithItem
{
    public void Initialize([MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint grfMode);
}
