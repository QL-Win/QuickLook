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
using QuickLook.Plugin.AppViewer.PackageParsers.Hap;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace QuickLook.Plugin.AppViewer.InfoPanels;

public partial class HapInfoPanel : UserControl, IAppInfoPanel
{
    private readonly ContextObject _context;

    public HapInfoPanel(ContextObject context)
    {
        _context = context;

        DataContext = this;
        InitializeComponent();

        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        applicationNameTitle.Text = TranslationHelper.Get("APP_NAME", translationFile);
        versionNameTitle.Text = TranslationHelper.Get("APP_VERSION_NAME", translationFile);
        versionCodeTitle.Text = TranslationHelper.Get("APP_VERSION_CODE", translationFile);
        bundleNameTitle.Text = TranslationHelper.Get("BUNDLE_NAME", translationFile);
        minimumAPIVersionTitle.Text = TranslationHelper.Get("APP_MIN_API_VERSION", translationFile);
        targetAPIVersionTitle.Text = TranslationHelper.Get("APP_TARGET_API_VERSION", translationFile);
        compileSdkVersionTitle.Text = TranslationHelper.Get("COMPILE_SDK_VERSION", translationFile);
        totalSizeTitle.Text = TranslationHelper.Get("TOTAL_SIZE", translationFile);
        modDateTitle.Text = TranslationHelper.Get("LAST_MODIFIED", translationFile);
        permissionsGroupBox.Header = TranslationHelper.Get("PERMISSIONS", translationFile);
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
                HapInfo hapInfo = HapParser.Parse(path);
                var last = File.GetLastWriteTime(path);

                Dispatcher.Invoke(() =>
                {
                    applicationName.Text = hapInfo.Label;
                    versionName.Text = hapInfo.VersionName;
                    versionCode.Text = hapInfo.VersionCode;
                    bundleName.Text = hapInfo.BundleName;
                    deviceTypes.Text = string.Join(", ", hapInfo.DeviceTypes);
                    minimumAPIVersion.Text = hapInfo.MinAPIVersion;
                    targetAPIVersion.Text = hapInfo.TargetAPIVersion;
                    compileSdkVersion.Text = $"{hapInfo.CompileSdkType} {hapInfo.CompileSdkVersion}";
                    totalSize.Text = size.ToPrettySize(2);
                    modDate.Text = last.ToString(CultureInfo.CurrentCulture);
                    permissions.ItemsSource = hapInfo.RequestPermissions;

                    if (hapInfo.HasIcon)
                    {
                        if (hapInfo.Logo?.Length > 0)
                        {
                            using var stream = new MemoryStream(hapInfo.Logo);
                            var icon = new BitmapImage();
                            icon.BeginInit();
                            icon.CacheOption = BitmapCacheOption.OnLoad;
                            icon.StreamSource = stream;
                            icon.EndInit();
                            icon.Freeze();
                            background.Source = null;
                            foreground.Source = icon;
                        }
                        else if (hapInfo.HasLayeredIcon)
                        {
                            {
                                using var stream = new MemoryStream(hapInfo.AppIconBackground);
                                var icon = new BitmapImage();
                                icon.BeginInit();
                                icon.CacheOption = BitmapCacheOption.OnLoad;
                                icon.StreamSource = stream;
                                icon.EndInit();
                                icon.Freeze();
                                background.Source = icon;
                            }
                            {
                                using var stream = new MemoryStream(hapInfo.AppIconForeground);
                                var icon = new BitmapImage();
                                icon.BeginInit();
                                icon.CacheOption = BitmapCacheOption.OnLoad;
                                icon.StreamSource = stream;
                                icon.EndInit();
                                icon.Freeze();
                                foreground.Source = icon;
                            }
                        }
                    }
                    else
                    {
                        background.Source = null;
                        foreground.Source = new BitmapImage(new Uri("pack://application:,,,/QuickLook.Plugin.AppViewer;component/Resources/harmonyos.png"));
                    }

                    _context.IsBusy = false;
                });
            }
        });
    }
}
