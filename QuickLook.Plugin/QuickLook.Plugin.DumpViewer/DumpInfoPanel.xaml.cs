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

using QuickLook.Common.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace QuickLook.Plugin.DumpViewer;

public partial class DumpInfoPanel : UserControl
{
    private readonly string _translationFile;
    private ICollectionView _moduleView;

    public DumpInfoPanel()
    {
        InitializeComponent();

        _translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");

        titleText.Text = Tr("MINIDUMP_FILE_SUMMARY", "Minidump File Summary");
        dumpSummaryHeader.Text = Tr("DUMP_SUMMARY", "Dump Summary");
        systemInformationHeader.Text = Tr("SYSTEM_INFORMATION", "System Information");
        modulesHeader.Text = Tr("MODULES", "Modules");
        searchPlaceholder.Text = Tr("SEARCH", "Search");
        moduleNameColumn.Header = Tr("MODULE_NAME", "Module Name");
        moduleVersionColumn.Header = Tr("MODULE_VERSION", "Module Version");
        modulePathColumn.Header = Tr("MODULE_PATH", "Module Path");
    }

    public void DisplayInfo(string path)
    {
        timeText.Text = File.GetLastWriteTime(path).ToString();
        dumpSummaryItems.ItemsSource = CreateLoadingItems(path);

        _ = Task.Run(() =>
        {
            var dumpInfo = MinidumpReader.Read(path);

            Dispatcher.Invoke(() => LoadDumpInfo(dumpInfo));
        });
    }

    private void LoadDumpInfo(DumpInfo info)
    {
        timeText.Text = (info.TimeStamp ?? info.LastWriteTime).ToString();

        var dumpSummary = new List<KeyValueItem>
        {
            new(Tr("DUMP_FILE", "Dump File"), $"{Path.GetFileName(info.FilePath)} : {info.FilePath}"),
            new(Tr("LAST_WRITE_TIME", "Last Write Time"), info.LastWriteTime.ToString()),
            new(Tr("PROCESS_NAME", "Process Name"), Missing(info.ProcessPath)),
            new(Tr("PROCESS_ARCHITECTURE", "Process Architecture"), Missing(info.Architecture)),
            new(Tr("EXCEPTION_CODE", "Exception Code"), Missing(info.ExceptionCode)),
            new(Tr("EXCEPTION_INFORMATION", "Exception Information"), info.ExceptionInformation ?? string.Empty),
            new(Tr("HEAP_INFORMATION", "Heap Information"), info.HasHeapInformation ? Tr("PRESENT", "Present") : string.Empty),
            new(Tr("ERROR_INFORMATION", "Error Information"), info.HasErrorInformation ? Tr("PRESENT", "Present") : string.Empty),
        };

        if (!string.IsNullOrEmpty(info.ParseError))
            dumpSummary.Add(new(Tr("PARSER_MESSAGE", "Parser Message"), info.ParseError));

        dumpSummaryItems.ItemsSource = dumpSummary;

        systemInformationItems.ItemsSource = new List<KeyValueItem>
        {
            new(Tr("OS_VERSION", "OS Version"), Missing(info.OSVersion)),
            new(Tr("CLR_VERSIONS", "CLR Version(s)"), Missing(info.ClrVersions)),
        };

        modulesGrid.ItemsSource = info.Modules;
        _moduleView = CollectionViewSource.GetDefaultView(modulesGrid.ItemsSource);
        _moduleView.Filter = FilterModule;
    }

    private bool FilterModule(object item)
    {
        if (item is not DumpModuleInfo module)
            return false;

        var searchText = searchTextBox.Text;
        if (string.IsNullOrWhiteSpace(searchText))
            return true;

        return Contains(module.Name, searchText)
            || Contains(module.Version, searchText)
            || Contains(module.Path, searchText);
    }

    private static bool Contains(string source, string value)
    {
        return source?.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) >= 0;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        searchPlaceholder.Visibility = string.IsNullOrEmpty(searchTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        _moduleView?.Refresh();
    }

    private List<KeyValueItem> CreateLoadingItems(string path)
    {
        return
        [
            new(Tr("DUMP_FILE", "Dump File"), $"{Path.GetFileName(path)} : {path}"),
            new(Tr("LAST_WRITE_TIME", "Last Write Time"), File.GetLastWriteTime(path).ToString()),
            new(Tr("PROCESS_NAME", "Process Name"), Tr("SEARCHING", "Searching...")),
        ];
    }

    private string Missing(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? Tr("NOT_FOUND", "not found") : value;
    }

    private string Tr(string key, string fallback)
    {
        return TranslationHelper.Get(key, _translationFile, failsafe: fallback);
    }
}
