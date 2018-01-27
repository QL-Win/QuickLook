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
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;
using QuickLook.Common.Helpers;

namespace QuickLook.Plugin.HtmlViewer
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class WebpagePanel : UserControl, IDisposable
    {
        public WebpagePanel()
        {
            InitializeComponent();

            browser.Zoom = (int) (100 * DpiHelper.GetCurrentScaleFactor().Vertical);
        }

        public void Dispose()
        {
            browser?.Dispose();
            browser = null;
        }

        public void Navigate(string path)
        {
            if (Path.IsPathRooted(path))
                path = Helper.FilePathToFileUrl(path);

            browser.Dispatcher.Invoke(() => { browser.Navigate(path); }, DispatcherPriority.Loaded);
        }

        public void LoadHtml(string html)
        {
            var s = new MemoryStream(Encoding.UTF8.GetBytes(html ?? ""));

            browser.Dispatcher.Invoke(() => { browser.Navigate(s); }, DispatcherPriority.Loaded);
        }
    }
}