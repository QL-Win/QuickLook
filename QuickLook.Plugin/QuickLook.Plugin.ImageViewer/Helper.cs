// Copyright © 2020 Paddy Xu
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

using System.Reflection;
using System.Windows.Media.Imaging;
using QuickLook.Common.Helpers;

namespace QuickLook.Plugin.ImageViewer
{
    internal class Helper
    {
        public static void DpiHack(BitmapSource img)
        {
            // a dirty hack... but is the fastest

            var newDpiX = (double) DpiHelper.DefaultDpi * DpiHelper.GetCurrentScaleFactor().Horizontal;
            var newDpiY = (double) DpiHelper.DefaultDpi * DpiHelper.GetCurrentScaleFactor().Vertical;

            var dpiX = img.GetType().GetField("_dpiX",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var dpiY = img.GetType().GetField("_dpiY",
                BindingFlags.NonPublic | BindingFlags.Instance);
            dpiX?.SetValue(img, newDpiX);
            dpiY?.SetValue(img, newDpiY);
        }
    }
}