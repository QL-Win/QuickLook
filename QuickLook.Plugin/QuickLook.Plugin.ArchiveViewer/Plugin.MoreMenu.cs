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

using QuickLook.Common.Commands;
using QuickLook.Common.Controls;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin.MoreMenu;
using QuickLook.Plugin.ArchiveViewer.ChromiumResourcePackage;
using QuickLook.Plugin.ArchiveViewer.CompoundFileBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WindowsAPICodePack.Dialogs;

namespace QuickLook.Plugin.ArchiveViewer;

public sealed partial class Plugin
{
    /// <summary>
    /// Command to extract archive contents to a directory. Executed asynchronously.
    /// </summary>
    public ICommand ExtractToDirectoryCommand { get; }

    /// <summary>
    /// Constructor - initializes commands used by the plugin.
    /// </summary>
    public Plugin()
    {
        ExtractToDirectoryCommand = new AsyncRelayCommand(ExtractToDirectoryAsync);
    }

    /// <summary>
    /// Return additional "More" menu items for the plugin.
    /// When the current file is an EIF archive, a menu item to extract to directory is provided.
    /// </summary>
    public IEnumerable<IMenuItem> GetMenuItems()
    {
        // Currently only supports for CFB and EIF files
        if (_path.EndsWith(".cfb", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".eif", StringComparison.OrdinalIgnoreCase)
            || _path.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
        {
            // Use external Translations.config shipped next to the executing assembly
            string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");

            yield return new MoreMenuItem()
            {
                Icon = FontSymbols.MoveToFolder,
                Header = TranslationHelper.Get("MW_ExtractToDirectory", translationFile),
                MenuItems = null,
                Command = ExtractToDirectoryCommand,
            };
        }
    }

    /// <summary>
    /// Show folder picker and extract archive contents to the chosen directory.
    /// For EIF files, prompt the user whether to apply EIF-specific Face.dat ordering.
    /// </summary>
    public async Task ExtractToDirectoryAsync()
    {
        using CommonOpenFileDialog dialog = new()
        {
            IsFolderPicker = true,
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            if (_path.EndsWith(".cfb", StringComparison.OrdinalIgnoreCase))
            {
                // Generic compound file extraction
                await Task.Run(() =>
                {
                    CompoundFileExtractor.ExtractToDirectory(_path, dialog.FileName);
                });
            }
            else if (_path.EndsWith(".eif", StringComparison.OrdinalIgnoreCase))
            {
                string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");

                // Ask the user whether to apply EIF-specific `Face.dat` ordering during extraction
                MessageBoxResult result = MessageBox.Show(TranslationHelper.Get("MW_ExtractToDirectory_EIFOrderFaceDat",
                    translationFile), "QuickLook", MessageBoxButton.YesNo, MessageBoxImage.Question);

                // If user chooses Yes, use EifExtractor which reorders images according to `Face.dat`
                if (result == MessageBoxResult.Yes)
                {
                    await Task.Run(() =>
                    {
                        EifExtractor.ExtractToDirectory(_path, dialog.FileName);
                    });
                }
                else
                {
                    // Fallback: generic compound file extraction
                    await Task.Run(() =>
                    {
                        CompoundFileExtractor.ExtractToDirectory(_path, dialog.FileName);
                    });
                }
            }
            else if (_path.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
            {
                // Chromium resource package file v5 extraction
                await Task.Run(() =>
                {
                    PakExtractor.ExtractToDirectory(_path, dialog.FileName);
                });
            }
        }
    }
}
