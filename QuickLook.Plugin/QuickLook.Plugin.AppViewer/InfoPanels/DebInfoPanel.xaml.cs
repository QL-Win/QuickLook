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
using QuickLook.Plugin.AppViewer.PackageParsers.Deb;
using QuickLook.Plugin.AppViewer.PackageParsers.Wgt;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace QuickLook.Plugin.AppViewer.InfoPanels;

public partial class DebInfoPanel : UserControl, IAppInfoPanel
{
    private readonly ContextObject _context;

    public DebInfoPanel(ContextObject context)
    {
        _context = context;

        DataContext = this;
        InitializeComponent();

        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        packageNameTitle.Text = TranslationHelper.Get("PACKAGE_NAME", translationFile);
        versionNameTitle.Text = TranslationHelper.Get("APP_VERSION_NAME", translationFile);
        maintainerTitle.Text = TranslationHelper.Get("MAINTAINER", translationFile);
        totalSizeTitle.Text = TranslationHelper.Get("TOTAL_SIZE", translationFile);
        modDateTitle.Text = TranslationHelper.Get("LAST_MODIFIED", translationFile);
        descriptionGroupBox.Header = TranslationHelper.Get("DESCRIPTION", translationFile);
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
                DebInfo debInfo = DebParser.Parse(path);
                var last = File.GetLastWriteTime(path);

                Dispatcher.Invoke(() =>
                {
                    packageName.Text = debInfo.Package;
                    versionName.Text = debInfo.Version;
                    maintainer.Text = debInfo.Maintainer;
                    totalSize.Text = size.ToPrettySize(2);
                    modDate.Text = last.ToString(CultureInfo.CurrentCulture);
                    description.Text = debInfo.Description;

                    _context.IsBusy = false;
                });
            }
        });
    }
}
