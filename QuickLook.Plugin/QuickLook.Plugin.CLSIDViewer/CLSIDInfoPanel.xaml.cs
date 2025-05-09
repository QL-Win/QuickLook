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

using QuickLook.Common.Plugin;
using System.Windows;
using System.Windows.Controls;

namespace QuickLook.Plugin.CLSIDViewer;

public partial class CLSIDInfoPanel : UserControl
{
    public CLSIDInfoPanel()
    {
        DataContext = this;
        InitializeComponent();
    }

    public void DisplayInfo(string path, ContextObject context)
    {
        Content = path.ToUpper() switch
        {
            CLSIDRegister.RecycleBin => new RecycleBinPanel(context),
            CLSIDRegister.ThisPC => new ThisPCPanel(context),
            _ => new TextBlock()
            {
                Text = $"Unsupported for {path}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
    }
}
