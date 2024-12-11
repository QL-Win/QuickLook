// Copyright © 2018 Paddy Xu
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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Interop;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;

namespace QuickLook.Helpers;

internal class ShareHelper
{
    private static string _sharingPath = string.Empty;

    internal static bool IsShareSupported(string path)
    {
        return !Directory.Exists(path) && App.IsWin10 && Environment.OSVersion.Version >= new Version("10.0.16299.0");
    }

    internal static void Share(string path, Window parent, bool forceOpenWith = false)
    {
        if (string.IsNullOrEmpty(path))
            return;

        _sharingPath = path;

        if (!forceOpenWith && IsShareSupported(path))
            ShowShareUI(parent);
        else
            ShowRunWithUI();
    }

    private static void ShowRunWithUI()
    {
        try
        {
            Process.Start(new ProcessStartInfo("rundll32.exe")
            {
                Arguments = $"shell32.dll,OpenAs_RunDLL {_sharingPath}",
                WorkingDirectory = Path.GetDirectoryName(_sharingPath)
            });
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
        }
    }

    private static void ShowShareUI(Window parent)
    {
        var hwnd = new WindowInteropHelper(parent).Handle;
        var dtm = DataTransferManagerHelper.GetForWindow(hwnd);
        dtm.DataRequested += OnShareDataRequested;

        DataTransferManagerHelper.ShowShareUIForWindow(hwnd);
    }

    private static async void OnShareDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
    {
        var deferral = args.Request.GetDeferral();
        try
        {
            var dp = args.Request.Data;
            dp.Properties.Title =
                $"“{Path.GetFileName(_sharingPath)}” ({new FileInfo(_sharingPath).Length.ToPrettySize()})";

            var filesToShare = new List<IStorageItem>();
            var imageFile = await StorageFile.GetFileFromPathAsync(_sharingPath);
            filesToShare.Add(imageFile);

            dp.SetStorageItems(filesToShare);
        }
        finally
        {
            _sharingPath = string.Empty;
            //sender.DataRequested -= OnShareDataRequested;
            deferral.Complete();
        }
    }
}

internal static class DataTransferManagerHelper
{
    private static readonly Guid DTM_IID =
        new Guid(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

    private static IDataTransferManagerInterop DataTransferManagerInterop =>
        (IDataTransferManagerInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(DataTransferManager));

    public static DataTransferManager GetForWindow(IntPtr hwnd)
    {
        return DataTransferManagerInterop.GetForWindow(hwnd, DTM_IID);
    }

    public static void ShowShareUIForWindow(IntPtr hwnd)
    {
        DataTransferManagerInterop.ShowShareUIForWindow(hwnd);
    }

    [ComImport]
    [Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDataTransferManagerInterop
    {
        DataTransferManager GetForWindow([In] IntPtr appWindow, [In] ref Guid riid);

        void ShowShareUIForWindow(IntPtr appWindow);
    }
}
