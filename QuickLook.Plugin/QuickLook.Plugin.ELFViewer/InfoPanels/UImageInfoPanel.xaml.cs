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

using ELFSharp.UImage;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ELFViewer.InfoPanels;

public partial class UImageInfoPanel : UserControl, IInfoPanel
{
    public UImageInfoPanel()
    {
        InitializeComponent();

        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        imageNameTitle.Text = TranslationHelper.Get("NAME", translationFile);
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
                var tried = UImageReader.TryLoad(path, out UImage uImage);

                Dispatcher.Invoke(() =>
                {
                    var arch = tried == UImageResult.OK ? uImage.Architecture.ToMachineName() : string.Empty;
                    architectureContainer.Visibility = string.IsNullOrEmpty(arch) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

                    if (tried == UImageResult.OK)
                    {
                        imageName.Text = uImage.Name;
                        architecture.Text = arch;
                        format.Text = $"UImage - {uImage.OperatingSystem} / {uImage.Compression}";
                        formatProfile.Text = $"{uImage.Type} / {uImage.Architecture}";
                    }
                    else
                    {
                        imageName.Text = string.Empty;
                        architecture.Text = string.Empty;
                        format.Text = tried.ToString();
                        formatProfile.Text = tried.ToString();
                    }

                    totalSize.Text = size.ToPrettySize(2);
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/QuickLook.Plugin.ELFViewer;component/Resources/uboot.png"));
                });
            }
        });
    }
}

file static class MachineTypeExtension
{
    public static string ToMachineName(this Architecture arch)
    {
        return arch switch
        {
            Architecture.Alpha => "Alpha",
            Architecture.ARM => "ARM",
            Architecture.Ix86 => "Ix86",
            Architecture.Itanium => "Itanium",
            Architecture.MIPS => "MIPS",
            Architecture.MIPS64 => "MIPS64",
            Architecture.PowerPC => "PowerPC",
            Architecture.S390 => "S390",
            Architecture.SuperH => "SuperH",
            Architecture.SPARC => "SPARC",
            Architecture.SPARC64 => "SPARC64",
            Architecture.M68k => "M68k",
            Architecture.MicroBlaze => "MicroBlaze",
            Architecture.Nios2 => "Nios2",
            Architecture.Blackfin => "Blackfin",
            Architecture.AVR32 => "AVR32",
            Architecture.ST200 => "ST200",
            Architecture.Sandbox => "Sandbox",
            Architecture.NDS32 => "NDS32",
            Architecture.OpenRISC => "OpenRISC",
            Architecture.Invalid or _ => "UNKNOWN",
        };
    }
}
