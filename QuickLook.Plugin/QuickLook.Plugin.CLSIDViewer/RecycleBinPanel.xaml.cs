// Copyright © 2017-2025 QL-Win Contributors
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
using System.Windows;
using System.Windows.Controls;

namespace QuickLook.Plugin.CLSIDViewer;

public partial class RecycleBinPanel : UserControl
{
    public RecycleBinPanel()
    {
        InitializeComponent();
        Loaded += OnRecycleBinPanelLoaded;
    }

    private void OnRecycleBinPanelLoaded(object sender, RoutedEventArgs e)
    {
        UpdateState();
    }

    private void OnEmptyRecycleBinClick(object sender, RoutedEventArgs e)
    {
        // TODO: Use async to avoid blocking the UI thread
        if (RecycleBinHelper.EmptyRecycleBin())
        {
            UpdateState();
        }
    }

    private void UpdateState()
    {
        bool hasTrash = RecycleBinHelper.HasTrash();

        EmptyRecycleBinButton.Visibility = hasTrash ? Visibility.Visible : Visibility.Collapsed;
        EmptyRecycleBinText.Visibility = hasTrash ? Visibility.Collapsed : Visibility.Visible;
    }
}

file static class RecycleBinHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SHQUERYRBINFO
    {
        public uint cbSize;
        public ulong i64Size;
        public ulong i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(nint hwnd, string pszRootPath, RecycleFlags dwFlags);

    [Flags]
    private enum RecycleFlags : uint
    {
        SHERB_NOCONFIRMATION = 0x00000001,
        SHERB_NOPROGRESSUI = 0x00000002,
        SHERB_NOSOUND = 0x00000004,
    }

    public static bool HasTrash()
    {
        var info = new SHQUERYRBINFO()
        {
            cbSize = (uint)Marshal.SizeOf(typeof(SHQUERYRBINFO))
        };

        int result = SHQueryRecycleBin(null, ref info);
        
        if (result == 0) // S_OK
        {
            return info.i64NumItems > 0;
        }

        // Fallback
        return false;
    }

    public static bool EmptyRecycleBin()
    {
        int result = SHEmptyRecycleBin(IntPtr.Zero, null,
            RecycleFlags.SHERB_NOCONFIRMATION | RecycleFlags.SHERB_NOPROGRESSUI | RecycleFlags.SHERB_NOSOUND);

        return result == 0;
    }
}
