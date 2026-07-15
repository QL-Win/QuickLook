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

using Prefetch;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuickLook.Plugin.PrefetchViewer;

public partial class PrefetchInfoPanel : UserControl
{
    public PrefetchInfoPanel()
    {
        InitializeComponent();

        var translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        pfVersionTitle.Text = TranslationHelper.Get("PF_VERSION", translationFile);
        fileHashTitle.Text = TranslationHelper.Get("FILE_HASH", translationFile);
        totalSizeTitle.Text = TranslationHelper.Get("TOTAL_SIZE", translationFile);
        runCountTitle.Text = TranslationHelper.Get("RUN_COUNT", translationFile);
        lastRunTitle.Text = TranslationHelper.Get("LAST_RUNS", translationFile);
        fileMetricsTitle.Text = TranslationHelper.Get("FILE_METRICS", translationFile);
        traceChainsTitle.Text = TranslationHelper.Get("TRACE_CHAINS", translationFile);
        volumeInfoTitle.Text = TranslationHelper.Get("VOLUME_INFO", translationFile);
        referencedFilesTitle.Text = TranslationHelper.Get("REFERENCED_FILES", translationFile);
    }

    public void LoadPrefetch(string path)
    {
        var name = Path.GetFileName(path);

        try
        {
            IPrefetch pf;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                pf = PrefetchFile.Open(fs, path);
            }

            Dispatcher.Invoke(() =>
            {
                pfVersion.Text = pf.Header.Version.ToString();
                fileHash.Text = pf.Header.Hash;
                totalSize.Text = ((long)pf.Header.FileSize).ToPrettySize(2);
                runCount.Text = pf.RunCount.ToString();

                foreach (var runTime in pf.LastRunTimes)
                {
                    var tb = new TextBlock
                    {
                        Text = runTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        Margin = new Thickness(0, 0, 0, 2)
                    };
                    lastRunList.Items.Add(tb);
                }

                fileMetrics.Text = $"{pf.FileMetrics.Count} entries";
                traceChains.Text = $"{pf.TraceChains.Count} entries";

                foreach (var vol in pf.VolumeInformation)
                {
                    var border = new Border
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Margin = new Thickness(0, 0, 0, 4),
                        Child = CreateVolumeInfoPanel(vol)
                    };
                    volumeInfoList.Items.Add(border);
                }

                foreach (var fn in pf.Filenames)
                {
                    var tb = new TextBlock
                    {
                        Text = fn,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Margin = new Thickness(0, 0, 0, 1)
                    };
                    referencedFilesList.Items.Add(tb);
                }
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                errorText.Text = $"Error: {ex.Message}";
                errorText.Visibility = Visibility.Visible;
            });
        }
    }

    private static StackPanel CreateVolumeInfoPanel(Prefetch.Other.VolumeInfo vol)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };

        panel.Children.Add(new TextBlock
        {
            Text = $"Device: {vol.DeviceName}",
            FontWeight = FontWeights.SemiBold
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Serial: {vol.SerialNumber}  |  Created: {vol.CreationTime:yyyy-MM-dd HH:mm:ss}"
        });

        if (vol.DirectoryNames.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = $"Directories ({vol.DirectoryNames.Count}):",
                Margin = new Thickness(0, 2, 0, 0)
            });
            foreach (var dir in vol.DirectoryNames)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"  {dir}",
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
            }
        }

        return panel;
    }
}
