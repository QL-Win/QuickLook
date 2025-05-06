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

using System.Windows;
using System.Windows.Controls;

namespace QuickLook.Plugin.CLSIDViewer;

public partial class CLSIDInfoPanel : UserControl
{
    public CLSIDInfoPanel()
    {
        InitializeComponent();
    }

    public void DisplayInfo(string path)
    {
        switch (path.ToUpper())
        {
            case "::{645FF040-5081-101B-9F08-00AA002F954E}"  // Recycle Bin
              or "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}": // This PC
                break;

            default:
                UnsupportedTextBlock.Text = $"Unsupported for {path}";
                UnsupportedTextBlock.Visibility = Visibility.Visible;
                break;
        }
    }
}
