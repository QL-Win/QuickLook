// Copyright © 2017 Paddy Xu
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

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QuickLook.Plugin.InfoPanel;

/// <summary>
///     Interaction logic for InfoPanel.xaml
/// </summary>
public partial class InfoPanel : UserControl
{
    private bool _stop;

    public InfoPanel()
    {
        InitializeComponent();

        // apply global theme
        Resources.MergedDictionaries[0].Clear();
    }

    public bool Stop
    {
        set => _stop = value;
        get => _stop;
    }

    public void DisplayInfo(string path)
    {
        Task.Run(() =>
        {
            var scale = DisplayDeviceHelper.GetCurrentScaleFactor();

            var icon =
                WindowsThumbnailProvider.GetThumbnail(path,
                    (int)(128 * scale.Horizontal),
                    (int)(128 * scale.Vertical),
                    ThumbnailOptions.ScaleUp);

            var source = icon?.ToBitmapSource();
            icon?.Dispose();

            Dispatcher.BeginInvoke(new Action(() => image.Source = source));
        });

        var name = Path.GetFileName(path);
        filename.Text = string.IsNullOrEmpty(name) ? path : name;

        var last = File.GetLastWriteTime(path);
        modDate.Text = string.Format(TranslationHelper.Get("InfoPanel_LastModified"),
            last.ToString(CultureInfo.CurrentCulture));

        Stop = false;

        Task.Run(() =>
        {
            if (File.Exists(path))
            {
                var size = new FileInfo(path).Length;

                Dispatcher.Invoke(() => { totalSize.Text = size.ToPrettySize(2); });
            }
            else if (Path.GetPathRoot(path) == path) // is this a drive?
            {
                long totalSpace;
                long totalFreeSpace;

                FileHelper.GetDriveSpace(path, out totalSpace, out totalFreeSpace);

                Dispatcher.Invoke(() =>
                {
                    totalSize.Text =
                        string.Format(TranslationHelper.Get("InfoPanel_DriveSize"),
                            totalSpace.ToPrettySize(2),
                            totalFreeSpace.ToPrettySize(2));
                });
            }
            else if (Directory.Exists(path))
            {
                FileHelper.CountFolder(path, ref _stop,
                    out var totalDirsL, out var totalFilesL, out var totalSizeL);

                if (!Stop)
                    Dispatcher.Invoke(() =>
                    {
                        string t;
                        var folders = totalDirsL == 0
                            ? string.Empty
                            : string.Format(TranslationHelper.Get(
                                totalDirsL == 1 ? "InfoPanel_Folder" : "InfoPanel_Folders"), totalDirsL);
                        var files = totalFilesL == 0
                            ? string.Empty
                            : string.Format(TranslationHelper.Get(
                                totalFilesL == 1 ? "InfoPanel_File" : "InfoPanel_Files"), totalFilesL);

                        if (!string.IsNullOrEmpty(folders) && !string.IsNullOrEmpty(files))
                            t = string.Format(
                                TranslationHelper.Get("InfoPanel_FolderAndFile"), folders, files);
                        else if (string.IsNullOrEmpty(folders) && string.IsNullOrEmpty(files))
                            t = string.Empty;
                        else
                            t = $"({folders}{files})";

                        totalSize.Text =
                            $"{totalSizeL.ToPrettySize(2)} {t}";
                    });
            }
        });
    }
}
