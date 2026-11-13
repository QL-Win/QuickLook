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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace QuickLook.Helpers;

/// <summary>
/// The actual Recycle Bin emptying routine used by the hidden `--empty-recycle-bin`
/// command line mode of QuickLook.exe. The plugin launches that mode as a detached
/// process, so emptying keeps running independently of the viewer window and of
/// whether another file gets previewed.
/// </summary>
public static class RecycleBinCleaner
{
    /// <summary>
    /// Cross-process mutex which prevents two helper processes (for example, one
    /// started by a movie-preview instance and one by the main instance) from
    /// running a concurrent empty operation.
    /// </summary>
    public const string EmptyingMutexName = @"QuickLook.RecycleBin.Emptying";

    [Flags]
    private enum RecycleBinFlags : uint
    {
        SHERB_NOCONFIRMATION = 0x00000001,
        SHERB_NOPROGRESSUI = 0x00000002,
        SHERB_NOSOUND = 0x00000004,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHQUERYRBINFO
    {
        public uint cbSize;
        public ulong i64Size;
        public ulong i64NumItems;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleBinFlags dwFlags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    /// <summary>
    /// Empties all Recycle Bin locations without any user interface.
    /// </summary>
    /// <returns>
    /// <c>true</c> when the Recycle Bin was emptied (or another helper process was already
    /// doing it); <c>false</c> when Windows reported a failure.
    /// </returns>
    public static bool EmptyRecycleBin()
    {
        return EmptyRecycleBin(out _);
    }

    /// <summary>
    /// Empties all Recycle Bin locations without any user interface.
    /// </summary>
    /// <param name="hresult">The HRESULT returned by SHEmptyRecycleBin (0 on success).</param>
    /// <returns>
    /// <c>true</c> when the Recycle Bin was emptied (or another helper process was already
    /// doing it); <c>false</c> when Windows reported a failure.
    /// </returns>
    public static bool EmptyRecycleBin(out int hresult)
    {
        hresult = 0;

        Mutex mutex = null;
        var ownsMutex = false;

        try
        {
            mutex = new Mutex(false, EmptyingMutexName);

            try
            {
                // Do not wait: a second helper should exit immediately instead of
                // starting a concurrent empty operation.
                ownsMutex = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                // The previous helper terminated while emptying. Take over.
                ownsMutex = true;
            }

            if (!ownsMutex)
            {
                // Another helper is already emptying the Recycle Bin. Wait for that operation to finish
                // so this process does not report completion while the deletion is still running (the
                // panel refreshes when this helper exits).
                try
                {
                    ownsMutex = mutex.WaitOne(TimeSpan.FromMinutes(10));
                }
                catch (AbandonedMutexException)
                {
                    // The other helper terminated while emptying, so the operation is over.
                    ownsMutex = true;
                }

                if (!ownsMutex)
                {
                    // The deletion is still running after the grace period: report a failure instead of
                    // claiming that the Recycle Bin is empty.
                    hresult = unchecked((int)0x80004005); // E_FAIL
                    return false;
                }

                return TryGetRecycleBinInfo(out _, out var remainingItems) && remainingItems == 0;
            }

            // SHEmptyRecycleBin returns E_UNEXPECTED (0x8000FFFF) when the Recycle Bin is
            // already empty; that is not a failure for our background job.
            if (TryGetRecycleBinInfo(out _, out var itemCount) && itemCount == 0)
            {
                hresult = 0;
                return true;
            }

            hresult = SHEmptyRecycleBin(
                IntPtr.Zero,
                null,
                RecycleBinFlags.SHERB_NOCONFIRMATION |
                RecycleBinFlags.SHERB_NOPROGRESSUI |
                RecycleBinFlags.SHERB_NOSOUND);

            // S_FALSE (1) means "nothing was deleted"; both results are a success for us.
            return hresult == 0 || hresult == 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            hresult = Marshal.GetHRForException(ex);
            return false;
        }
        finally
        {
            if (ownsMutex)
            {
                try
                {
                    mutex.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }

            mutex?.Dispose();
        }
    }

    /// <summary>
    /// Queries the current Recycle Bin size and item count.
    /// </summary>
    /// <param name="totalSize">Total size in bytes.</param>
    /// <param name="totalCount">Number of items.</param>
    /// <returns><c>true</c> when the query succeeded.</returns>
    public static bool TryGetRecycleBinInfo(out ulong totalSize, out ulong totalCount)
    {
        totalSize = 0;
        totalCount = 0;

        try
        {
            var info = new SHQUERYRBINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(SHQUERYRBINFO)),
            };

            if (SHQueryRecycleBin(null, ref info) != 0) // S_OK (0)
                return false;

            totalSize = info.i64Size;
            totalCount = info.i64NumItems;

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return false;
        }
    }
}
