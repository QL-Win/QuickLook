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
using QuickLook.Common.Plugin;
using QuickLook.Plugin.AppViewer.PackageParsers.Msp;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QuickLook.Plugin.AppViewer.InfoPanels;

public partial class MspInfoPanel : UserControl, IAppInfoPanel
{
    private readonly ContextObject _context;

    public MspInfoPanel(ContextObject context)
    {
        _context = context;

        InitializeComponent();

        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        productNameTitle.Text = TranslationHelper.Get("PRODUCT_NAME", translationFile);
        // MSP has no product version; show the patch description instead.
        productVersionTitle.Text = TranslationHelper.Get("DESCRIPTION", translationFile);
        manufacturerTitle.Text = TranslationHelper.Get("MANUFACTURER", translationFile);
        totalSizeTitle.Text = TranslationHelper.Get("TOTAL_SIZE", translationFile);
        modDateTitle.Text = TranslationHelper.Get("LAST_MODIFIED", translationFile);
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
                var size = new FileInfo(path).Length;
                MspInfo mspInfo = MspParser.Parse(path);
                var last = File.GetLastWriteTime(path);

                Dispatcher.Invoke(() =>
                {
                    productName.Text = mspInfo.DisplayName;
                    productVersion.Text = mspInfo.Description;
                    manufacturer.Text = mspInfo.Manufacturer;
                    totalSize.Text = size.ToPrettySize(2);
                    modDate.Text = last.ToString(CultureInfo.CurrentCulture);

                    _context.IsBusy = false;
                });
            }
        });
    }
}
