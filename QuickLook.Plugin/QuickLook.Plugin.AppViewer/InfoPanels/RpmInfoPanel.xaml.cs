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

using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.AppViewer.PackageParsers.AppImage;
using QuickLook.Plugin.AppViewer.PackageParsers.Rpm;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.AppViewer.InfoPanels;

public partial class RpmInfoPanel : UserControl, IAppInfoPanel
{
    private readonly ContextObject _context;

    public RpmInfoPanel(ContextObject context)
    {
        _context = context;

        DataContext = this;
        InitializeComponent();

        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        applicationNameTitle.Text = TranslationHelper.Get("APP_NAME", translationFile);
        versionTitle.Text = TranslationHelper.Get("APP_VERSION", translationFile);
        architectureTitle.Text = TranslationHelper.Get("ARCHITECTURE", translationFile);
        typeTitle.Text = TranslationHelper.Get("TYPE", translationFile);
        terminalTitle.Text = TranslationHelper.Get("TERMINAL", translationFile);
        totalSizeTitle.Text = TranslationHelper.Get("TOTAL_SIZE", translationFile);
        modDateTitle.Text = TranslationHelper.Get("LAST_MODIFIED", translationFile);
        environmentGroupBox.Header = TranslationHelper.Get("ENVIRONMENT", translationFile);
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
                RpmInfo rpmInfo = RpmParser.Parse(path);
                var last = File.GetLastWriteTime(path);

                Dispatcher.Invoke(() =>
                {
                    applicationName.Text = rpmInfo.Name;
                    version.Text = rpmInfo.Version;
                    architectureName.Text = rpmInfo.Arch;
                    type.Text = rpmInfo.Type;
                    terminal.Text = rpmInfo.Terminal;
                    totalSize.Text = size.ToPrettySize(2);
                    modDate.Text = last.ToString(CultureInfo.CurrentCulture);
                    permissions.ItemsSource = rpmInfo.Env;

                    if (rpmInfo.HasIcon)
                    {
                        image.Source = rpmInfo.Logo.ToBitmapSource();
                    }
                    else
                    {
                        image.Source = new BitmapImage(new Uri("pack://application:,,,/QuickLook.Plugin.AppViewer;component/Resources/rpm.png"));
                    }

                    _context.IsBusy = false;
                });
            }
        });
    }
}
