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

using QuickLook.Common.Commands;
using QuickLook.Common.Controls;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin.MoreMenu;
using QuickLook.Plugin.ArchiveViewer.CompoundFileBinary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using WindowsAPICodePack.Dialogs;

namespace QuickLook.Plugin.ArchiveViewer;

public partial class Plugin
{
    public ICommand ExtractToDirectoryCommand { get; }

    public Plugin()
    {
        ExtractToDirectoryCommand = new AsyncRelayCommand(ExtractToDirectoryAsync);
    }

    public IEnumerable<IMenuItem> GetMenuItems()
    {
        if (_path.EndsWith(".eif", StringComparison.OrdinalIgnoreCase))
        {
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

    public async Task ExtractToDirectoryAsync()
    {
        using CommonOpenFileDialog dialog = new()
        {
            IsFolderPicker = true,
        };

        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            await Task.Run(() =>
            {
                if (_path.EndsWith(".cfb", StringComparison.OrdinalIgnoreCase))
                {
                    CompoundFileExtractor.ExtractToDirectory(_path, dialog.FileName);
                }
                else if (_path.EndsWith(".eif", StringComparison.OrdinalIgnoreCase))
                {
                    CompoundFileExtractor.ExtractToDirectory(_path, dialog.FileName);

                    string faceDatPath = Path.Combine(dialog.FileName, "face.dat");

                    if (File.Exists(faceDatPath))
                    {
                        Dictionary<string, Dictionary<string, int>> faceDat = FaceDatDecoder.Decode(File.ReadAllBytes(faceDatPath));
                        _ = faceDat;
                    }
                }
            });
        }
    }
}
