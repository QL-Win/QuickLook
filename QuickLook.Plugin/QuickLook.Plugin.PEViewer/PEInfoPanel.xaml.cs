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

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using QuickLook.Plugin.PEViewer.PEImageParser;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QuickLook.Plugin.PEViewer;

public partial class PEInfoPanel : UserControl
{
    public PEInfoPanel()
    {
        InitializeComponent();

        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        totalSizeTitle.Text = TranslationHelper.Get("TOTAL_SIZE", translationFile);
        fileVersionTitle.Text = TranslationHelper.Get("FILE_VERSION", translationFile);
        productVersionTitle.Text = TranslationHelper.Get("PRODUCT_VERSION", translationFile);
    }

    public void DisplayInfo(string path)
    {
        _ = Task.Run(() =>
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

        _ = Task.Run(() =>
        {
            if (File.Exists(path))
            {
                var info = FileVersionInfo.GetVersionInfo(path);
                var size = new FileInfo(path).Length;
                var arch = default(string);

                try
                {
                    int maxAttempts = 3;
                    int bufferSize = 1024;

                    for (int attempt = 0; attempt < maxAttempts; attempt++)
                    {
                        try
                        {
                            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                            using var binaryReader = new BinaryReader(stream);
                            var byteArray = binaryReader.ReadBytes(bufferSize);
                            var peImage = PEImage.FromBinary(byteArray);
                            var machine = peImage.CoffHeader.Machine;

                            arch = machine.ToImageMachineName();
                            break; // Successfully parsed, jumped out of the loop
                        }
                        catch (Exception e) when (e.Message == "Section headers incomplete.")
                        {
                            // Extended buffer size
                            bufferSize *= 2;
                        }
                        catch
                        {
                            // Non-Section headers errors will not be retryed
                            break;
                        }
                    }
                }
                catch
                {
                    // Usually because DOS Header not found
                }

                Dispatcher.Invoke(() =>
                {
                    architectureContainer.Visibility = string.IsNullOrEmpty(arch) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
                    architecture.Text = arch;
                    fileVersion.Text = info.FileVersion;
                    productVersion.Text = info.ProductVersion;
                    totalSize.Text = size.ToPrettySize(2);
                });
            }
        });
    }
}
