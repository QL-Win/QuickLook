// Copyright © 2017 Paddy Xu
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

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.HtmlViewer;
using UtfUnknown;

namespace QuickLook.Plugin.MarkdownViewer
{
    public class Plugin : IViewer
    {
        private WebpagePanel? _panel;
        private static string _resourcePath;
        private static readonly string ResourcePrefix = "QuickLook.Plugin.MarkdownViewer.Resources.";
        private string? _currentHtmlPath;

        private static bool OverrideFilesInDevelopment => true && Debugger.IsAttached; // Debug setting

        static Plugin()
        {
            // Set up resource path in AppData
            _resourcePath = Path.Combine(SettingHelper.LocalDataPath, "QuickLook.Plugin.MarkdownViewer");
        }

        public int Priority => 0;

        public void Init()
        {
            // Create directory if it doesn't exist
            if (!Directory.Exists(_resourcePath) || OverrideFilesInDevelopment)
            {
                Directory.CreateDirectory(_resourcePath);
                ExtractResources();
            }

            // Clean up any temporary HTML files if QuickLook was forcibly terminated
            try
            {
                var tempFiles = Directory.GetFiles(_resourcePath, "temp_*.html");
                foreach (var file in tempFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException) { } // Ignore deletion errors
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to clean up temporary HTML files: {ex.Message}");
            }
        }

        private void ExtractResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNames)
            {
                if (!resourceName.StartsWith(ResourcePrefix)) continue;

                var relativePath = resourceName.Substring(ResourcePrefix.Length);
                if (relativePath.Equals("resources", StringComparison.OrdinalIgnoreCase)) continue; // Skip 'resources' binary file

                var targetPath = Path.Combine(_resourcePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

                // Create directory if it doesn't exist
                var directory = Path.GetDirectoryName(targetPath);
                if (directory != null)
                    Directory.CreateDirectory(directory);

                // Extract the resource (skip if it already exists, unless in debug mode)
                if (File.Exists(targetPath) && !OverrideFilesInDevelopment)
                    continue;

                using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
                using (var fileStream = File.Create(targetPath))
                {
                    resourceStream?.CopyTo(fileStream);
                }
            }

            // Verify that md2html.html was extracted
            var htmlPath = Path.Combine(_resourcePath, "md2html.html");
            if (!File.Exists(htmlPath))
            {
                throw new FileNotFoundException($"Required template file md2html.html not found in resources. Available resources: {string.Join(", ", resourceNames)}");
            }
        }

        public bool CanHandle(string path)
        {
            return !Directory.Exists(path) && new[] { ".md", ".rmd", ".markdown" }.Any(path.ToLower().EndsWith);
        }

        public void Prepare(string path, ContextObject context)
        {
            context.PreferredSize = new Size(1000, 600);
        }

        public void View(string path, ContextObject context)
        {
            _panel = new WebpagePanel();
            context.ViewerContent = _panel;
            context.Title = Path.GetFileName(path);

            var htmlPath = GenerateMarkdownHtml(path);
            _panel.NavigateToFile(htmlPath);
            _panel.Dispatcher.Invoke(() => { context.IsBusy = false; }, DispatcherPriority.Loaded);

            _panel._webView.NavigationStarting += NavigationStarting_CancelNavigation;
        }

        private void NavigationStarting_CancelNavigation(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.StartsWith("data:")) // when using NavigateToString
                return;

            var newUri = new Uri(e.Uri);
            if (newUri == _panel?._currentUri) return;
            e.Cancel = true;

            // Open in default browser
            try
            {
                if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
                {
                    Debug.WriteLine($"Invalid URI format: {e.Uri}");
                    return;
                }

                // Safe schemes can open directly
                if (uri.Scheme == Uri.UriSchemeHttp ||
                    uri.Scheme == Uri.UriSchemeHttps ||
                    uri.Scheme == Uri.UriSchemeMailto)
                {
                    Process.Start(uri.AbsoluteUri);
                    return;
                }

                // Ask user for unsafe schemes. Use dispatcher to avoid blocking thread.
                var associatedApp = GetAssociatedAppForScheme(uri.Scheme);
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var result = MessageBox.Show(
                        !string.IsNullOrEmpty(associatedApp) ?
                        $"The following link will open in {associatedApp}:\n{e.Uri}" : $"The following link will open:\n{e.Uri}",
                        !string.IsNullOrEmpty(associatedApp) ?
                        $"Open {associatedApp}?" : "Open custom URI?",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(e.Uri);
                    }
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open URL in browser: {ex.Message}");
            }
        }

        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern uint AssocQueryString(
            AssocF flags,
            AssocStr str,
            string pszAssoc,
            string? pszExtra,
            [Out] StringBuilder? pszOut,
            ref uint pcchOut);

        [Flags]
        private enum AssocF
        {
            None = 0,
            VerifyExists = 0x1
        }

        private enum AssocStr
        {
            Command = 1,
            Executable = 2,
            FriendlyAppName = 4
        }

        private string? GetAssociatedAppForScheme(string scheme)
        {
            try
            {
                // Try to get friendly app name first
                uint pcchOut = 0;
                AssocQueryString(AssocF.None, AssocStr.FriendlyAppName, scheme, null, null, ref pcchOut);

                if (pcchOut > 0)
                {
                    StringBuilder pszOut = new StringBuilder((int)pcchOut);
                    AssocQueryString(AssocF.None, AssocStr.FriendlyAppName, scheme, null, pszOut, ref pcchOut);

                    var appName = pszOut.ToString().Trim();
                    if (!string.IsNullOrEmpty(appName))
                        return appName;
                }

                // Fall back to executable name if friendly name is not available
                pcchOut = 0;
                AssocQueryString(AssocF.None, AssocStr.Executable, scheme, null, null, ref pcchOut);

                if (pcchOut > 0)
                {
                    StringBuilder pszOut = new StringBuilder((int)pcchOut);
                    AssocQueryString(AssocF.None, AssocStr.Executable, scheme, null, pszOut, ref pcchOut);

                    var exeName = pszOut.ToString().Trim();
                    if (!string.IsNullOrEmpty(exeName))
                        return Path.GetFileName(exeName);
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get associated app: {ex.Message}");
                return null;
            }
        }

        private string GenerateMarkdownHtml(string path)
        {
            var templatePath = Path.Combine(_resourcePath, "md2html.html");

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Required template file md2html.html not found in extracted resources at {templatePath}");

            var bytes = File.ReadAllBytes(path);
            var encoding = CharsetDetector.DetectFromBytes(bytes).Detected?.Encoding ?? Encoding.Default;
            var content = encoding.GetString(bytes);

            var template = File.ReadAllText(templatePath);
            var html = template.Replace("{{content}}", content);

            // Generate unique filename and ensure it doesn't exist
            string outputPath;
            do
            {
                var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
                var outputFileName = $"temp_{uniqueId}.html";
                outputPath = Path.Combine(_resourcePath, outputFileName);
            } while (File.Exists(outputPath));

            // Clean up previous file if it exists
            CleanupTempHtmlFile();

            File.WriteAllText(outputPath, html);
            _currentHtmlPath = outputPath;

            return outputPath;
        }

        private void CleanupTempHtmlFile()
        {
            if (!string.IsNullOrEmpty(_currentHtmlPath) && File.Exists(_currentHtmlPath))
            {
                try
                {
                    File.Delete(_currentHtmlPath);
                }
                catch (IOException) { } // Ignore deletion errors
            }
        }

        public void Cleanup()
        {
            GC.SuppressFinalize(this);

            CleanupTempHtmlFile();

            _panel?.Dispose();
            _panel = null;
        }
    }
}