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

using ELFSharp.ELF;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.ELFViewer.InfoPanels;

public partial class ELFInfoPanel : UserControl, IInfoPanel
{
    public ELFInfoPanel()
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
                var isElf = ELFReader.TryLoad(path, out var elf);
                var arch = elf?.Machine.ToMachineName();
                var typeLogo = elf?.Type switch
                {
                    FileType.Executable => "exec",
                    FileType.SharedObject => "dyn",
                    FileType.None or FileType.Relocatable or FileType.Core or _ => "none",
                };

                Dispatcher.Invoke(() =>
                {
                    architectureContainer.Visibility = string.IsNullOrEmpty(arch) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
                    architecture.Text = arch;
                    format.Text = "ELF";
                    formatProfile.Text = elf != null ? $"{elf.Type} / {elf.Machine}" : "NotELF";
                    totalSize.Text = size.ToPrettySize(2);
                    image.Source = new BitmapImage(new Uri($"pack://application:,,,/QuickLook.Plugin.ELFViewer;component/Resources/{typeLogo}.png"));
                });
            }
        });
    }
}

file static class MachineTypeExtension
{
    public static string ToMachineName(this Machine machine)
    {
        // https://en.wikipedia.org/wiki/Executable_and_Linkable_Format
        return machine switch
        {
            Machine.M32 => "M32",
            Machine.SPARC => "SPARC",
            Machine.Intel386 => "x86",
            Machine.M68K => "M68K",
            Machine.M88K => "M88K",
            Machine.Intel486 => "Intel486",
            Machine.Intel860 => "Intel860",
            Machine.MIPS => "MIPS",
            Machine.S370 => "S370",
            Machine.MIPSRS3LE => "MIPSRS3LE",
            Machine.PARISC => "PA-RISC",
            Machine.VPP500 => "VPP500",
            Machine.SPARC32Plus => "SPARCv8+",
            Machine.Intel960 => "Intel960",
            Machine.PPC => "PowerPC",
            Machine.PPC64 => "PowerPC64",
            Machine.S390 => "S390",
            Machine.SPU => "SPU",
            Machine.V800 => "V800",
            Machine.FR20 => "FR20",
            Machine.RH32 => "RH-32",
            Machine.RCE => "RCE",
            Machine.ARM => "ARM",
            Machine.Alpha => "Alpha",
            Machine.SuperH => "SuperH",
            Machine.SPARCv9 => "SPARCv9",
            Machine.TriCore => "TriCore",
            Machine.ARC => "Argonaut RISC Core",
            Machine.H8300 => "H8300",
            Machine.H8300H => "H8300H",
            Machine.H8S => "H8S",
            Machine.H8500 => "H8500",
            Machine.IA64 => "IA64",
            Machine.MIPSX => "MIPSX",
            Machine.ColdFire => "ColdFire",
            Machine.M68HC12 => "M68HC12",
            Machine.MMA => "MMA",
            Machine.PCP => "PCP",
            Machine.NCPU => "RISC",
            Machine.NDR1 => "NDR1",
            Machine.StarCore => "Star*Core",
            Machine.ME16 => "ME16",
            Machine.ST100 => "ST100",
            Machine.TinyJ => "TinyJ",
            Machine.AMD64 => "x64",
            Machine.PDSP => "PDSP",
            Machine.PDP10 => "PDP10",
            Machine.PDP11 => "PDP11",
            Machine.FX66 => "FX66",
            Machine.ST9PLUS => "ST9PLUS",
            Machine.ST7 => "ST7",
            Machine.M68HC16 => "M68HC16",
            Machine.M68HC11 => "M68HC11",
            Machine.M68HC08 => "M68HC08",
            Machine.M68HC05 => "M68HC05",
            Machine.SVX => "SVx",
            Machine.ST19 => "ST19",
            Machine.VAX => "VAX",
            Machine.CRIS => "CRIS",
            Machine.Javelin => "Javelin",
            Machine.FirePath => "FirePath",
            Machine.ZSP => "ZSP",
            Machine.MMIX => "MMIX",
            Machine.HUANY => "HUANY",
            Machine.PRISM => "PRISM",
            Machine.AVR => "AVR",
            Machine.FR30 => "FR30",
            Machine.D10V => "D10V",
            Machine.D30V => "D30V",
            Machine.V850 => "V850",
            Machine.M32R => "M32R",
            Machine.MN10300 => "MN10300",
            Machine.MN10200 => "MN10200",
            Machine.PicoJava => "PicoJava",
            Machine.OpenRISC => "OpenRISC",
            Machine.ARCompact => "ARCompact",
            Machine.Xtensa => "Xtensa",
            Machine.VideoCore => "VideoCore",
            Machine.TMMGPP => "TMMGPP",
            Machine.NS32K => "NS32K",
            Machine.TPC => "TPC",
            Machine.SNP1k => "SNP1k",
            Machine.ST200 => "ST200",
            Machine.IP2K => "IP2K",
            Machine.MAX => "MAX",
            Machine.CompactRISC => "CompactRISC",
            Machine.F2MC16 => "F2MC16",
            Machine.MSP430 => "MSP430",
            Machine.Blackfin => "Blackfin",
            Machine.S1C33 => "S1C33",
            Machine.SEP => "SEP",
            Machine.ArcaRISC => "ArcaRISC",
            Machine.UNICORE => "UNICORE",
            Machine.Excess => "Excess",
            Machine.DXP => "DXP",
            Machine.AlteraNios2 => "AlteraNios2",
            Machine.CRX => "CRX",
            Machine.XGATE => "XGATE",
            Machine.C166 => "C166",
            Machine.M16C => "M16C",
            Machine.DSPIC30F => "DSPIC30F",
            Machine.EngineRISC => "EngineRISC",
            Machine.M32C => "M32C",
            Machine.TSK3000 => "TSK3000",
            Machine.RS08 => "RS08",
            Machine.SHARC => "SHARC",
            Machine.ECOG2 => "ECOG2",
            Machine.Score7 => "Score7",
            Machine.DSP24 => "DSP24",
            Machine.VideoCore3 => "VideoCore3",
            Machine.LatticeMico32 => "LatticeMico32",
            Machine.SeikoEpsonC17 => "SeikoEpsonC17",
            Machine.TIC6000 => "TIC6000",
            Machine.TIC2000 => "TIC2000",
            Machine.TIC5500 => "TIC5500",
            Machine.MMDSPPlus => "MMDSPPlus",
            Machine.CypressM8C => "CypressM8C",
            Machine.R32C => "R32C",
            Machine.TriMedia => "TriMedia",
            Machine.Hexagon => "Hexagon",
            Machine.Intel8051 => "Intel8051",
            Machine.STxP7x => "STxP7x",
            Machine.NDS32 => "NDS32",
            Machine.ECOG1 or Machine.ECOG1X => "ECOG1",
            Machine.MAXQ30 => "MAXQ30",
            Machine.XIMO16 => "XIMO16",
            Machine.MANIK => "MANIK",
            Machine.CrayNV2 => "CrayNV2",
            Machine.RX => "RX",
            Machine.METAG => "METAG",
            Machine.MCSTElbrus => "MCSTElbrus",
            Machine.ECOG16 => "ECOG16",
            Machine.CR16 => "CR16",
            Machine.ETPU => "ETPU",
            Machine.SLE9X => "SLE9X",
            Machine.L10M => "L10M",
            Machine.K10M => "K10M",
            Machine.AArch64 => "ARM64",
            Machine.AVR32 => "AVR32",
            Machine.STM8 => "STM8",
            Machine.TILE64 => "TILE64",
            Machine.TILEPro => "TILEPro",
            Machine.CUDA => "CUDA",
            Machine.TILEGx => "TILEGx",
            Machine.CloudShield => "CloudShield",
            Machine.CoreA1st => "CoreA1st",
            Machine.CoreA2nd => "CoreA2nd",
            Machine.ARCompact2 => "ARCompactV2",
            Machine.Open8 => "Open8",
            Machine.RL78 => "RL78",
            Machine.VideoCore5 => "VideoCore5",
            Machine.R78KOR => "R78KOR",
            Machine.F56800EX => "F56800EX",
            Machine.None or _ => "UNKNOWN",
        };
    }
}
