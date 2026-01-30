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

using ELFSharp.MachO;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ELFViewer.InfoPanels;

public partial class MachOInfoPanel : UserControl, IInfoPanel
{
    public MachOInfoPanel()
    {
        InitializeComponent();

        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        totalSizeTitle.Text = TranslationHelper.Get("TOTAL_SIZE", translationFile);
        formatTitle.Text = TranslationHelper.Get("FORMAT", translationFile);
        formatProfileTitle.Text = TranslationHelper.Get("FORMAT_PROFILE", translationFile);
    }

    public void DisplayInfo(string path)
    {
        var name = Path.GetFileName(path);
        filename.Text = string.IsNullOrEmpty(name) ? path : name;

        _ = Task.Run(() =>
        {
            if (File.Exists(path))
            {
                var size = new FileInfo(path).Length;
                var tried = MachOReader.TryLoad(path, out MachO machO);
                var arch = string.Empty;
                var profile = string.Empty;

                if (tried == MachOResult.OK)
                {
                    arch = machO.Machine.ToMachineName();
                    profile = machO.Machine.ToString();
                }
                else if (tried == MachOResult.FatMachO)
                {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                    if (MachOReader.TryLoadFat(stream, shouldOwnStream: true, out var machOs) == MachOResult.FatMachO)
                    {
                        arch = string.Join(", ", machOs.Select(m => m.Machine.ToMachineName()).Distinct());
                        profile = string.Join(", ", machOs.Select(m => m.Machine.ToString()).Distinct());
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    architectureContainer.Visibility = string.IsNullOrEmpty(arch) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
                    architecture.Text = arch;
                    format.Text = "DynamicLibrary";
                    formatProfile.Text = profile;
                    totalSize.Text = size.ToPrettySize(2);
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/QuickLook.Plugin.ELFViewer;component/Resources/dyn.png"));
                });
            }
        });
    }
}

file static class MachineTypeExtension
{
    public static string ToMachineName(this Machine machine)
    {
        return machine switch
        {
            Machine.Vax => "Vax",
            Machine.M68k => "M68k",
            Machine.X86 => "x86",
            Machine.X86_64 => "x64",
            Machine.M98k => "M98k",
            Machine.PaRisc => "PaRisc",
            Machine.Arm => "Arm",
            Machine.Arm64 => "Arm64",
            Machine.M88k => "M88k",
            Machine.Sparc => "Sparc",
            Machine.I860 => "I860",
            Machine.PowerPc => "PowerPc",
            Machine.PowerPc64 => "PowerPc64",
            Machine.Any or _ => "UNKNOWN",
        };
    }
}
