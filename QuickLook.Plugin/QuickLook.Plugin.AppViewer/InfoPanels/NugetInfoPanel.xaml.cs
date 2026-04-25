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
using QuickLook.Plugin.AppViewer.PackageParsers.Nuget;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace QuickLook.Plugin.AppViewer.InfoPanels;

public partial class NugetInfoPanel : UserControl, IAppInfoPanel
{
    private readonly ContextObject _context;

    public NugetInfoPanel(ContextObject context)
    {
        _context = context;

        DataContext = this;
        InitializeComponent();

        string translationFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Translations.config");
        packageIdTitle.Text = TranslationHelper.Get("NUGET_PACKAGE_ID", translationFile);
        versionTitle.Text = TranslationHelper.Get("APP_VERSION", translationFile);
        authorsTitle.Text = TranslationHelper.Get("NUGET_AUTHORS", translationFile);
        licenseTitle.Text = TranslationHelper.Get("NUGET_LICENSE", translationFile);
        projectUrlTitle.Text = TranslationHelper.Get("NUGET_PROJECT_URL", translationFile);
        repoUrlTitle.Text = TranslationHelper.Get("NUGET_REPO_URL", translationFile);
        frameworksTitle.Text = TranslationHelper.Get("NUGET_FRAMEWORKS", translationFile);
        totalSizeTitle.Text = TranslationHelper.Get("TOTAL_SIZE", translationFile);
        modDateTitle.Text = TranslationHelper.Get("LAST_MODIFIED", translationFile);
        dependenciesGroupBox.Header = TranslationHelper.Get("NUGET_DEPENDENCIES", translationFile);
        descriptionGroupBox.Header = TranslationHelper.Get("DESCRIPTION", translationFile);
    }

    public void DisplayInfo(string path)
    {
        var name = Path.GetFileName(path);
        filename.Text = string.IsNullOrEmpty(name) ? path : name;

        _ = Task.Run(() =>
        {
            if (!File.Exists(path)) return;

            var size = new FileInfo(path).Length;
            NugetInfo nugetInfo = NugetParser.Parse(path);
            var last = File.GetLastWriteTime(path);

            Dispatcher.Invoke(() =>
            {
                packageId.Text = nugetInfo.PackageId ?? string.Empty;
                version.Text = nugetInfo.Version ?? string.Empty;
                authors.Text = nugetInfo.Authors ?? string.Empty;

                // License
                licenseBlock.Text = nugetInfo.License ?? string.Empty;

                // Project URL
                if (!string.IsNullOrWhiteSpace(nugetInfo.ProjectUrl) &&
                    Uri.TryCreate(nugetInfo.ProjectUrl, UriKind.Absolute, out var projectUri))
                {
                    projectUrlText.Text = nugetInfo.ProjectUrl;
                    projectUrlLink.NavigateUri = projectUri;
                    projectUrlTitle.Visibility = Visibility.Visible;
                    projectUrlBlock.Visibility = Visibility.Visible;
                }

                // Source Repository URL
                if (!string.IsNullOrWhiteSpace(nugetInfo.RepositoryUrl) &&
                    Uri.TryCreate(nugetInfo.RepositoryUrl, UriKind.Absolute, out var repoUri))
                {
                    repoUrlText.Text = nugetInfo.RepositoryUrl;
                    repoUrlLink.NavigateUri = repoUri;
                    repoUrlTitle.Visibility = Visibility.Visible;
                    repoUrlBlock.Visibility = Visibility.Visible;
                }

                // Target Frameworks
                frameworks.Text = nugetInfo.TargetFrameworks?.Length > 0
                    ? string.Join(", ", nugetInfo.TargetFrameworks)
                    : string.Empty;

                totalSize.Text = size.ToPrettySize(2);
                modDate.Text = last.ToString(CultureInfo.CurrentCulture);

                // Description
                description.Text = nugetInfo.Description ?? string.Empty;

                // Dependencies
                if (nugetInfo.Dependencies?.Length > 0)
                {
                    dependencies.ItemsSource = nugetInfo.Dependencies;
                }

                // Icon — use embedded icon if available, otherwise keep default nuget.png
                if (nugetInfo.Icon != null)
                {
                    using var icon = nugetInfo.Icon;
                    image.Source = icon.ToBitmapSource();
                }

                _context.IsBusy = false;
            });
        });
    }

    private void OnHyperlinkNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch
        {
            // Ignore if the browser cannot be launched
        }
        e.Handled = true;
    }
}
