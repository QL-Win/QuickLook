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
using System.Windows.Controls;

namespace QuickLook.Plugin.ArchiveViewer;

/// <summary>
///     Interaction logic for ArchiveFileListView.xaml
/// </summary>
public partial class ArchiveFileListView : UserControl, IDisposable
{
    public ArchiveFileListView()
    {
        InitializeComponent();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        IconManager.ClearCache();
    }

    public void SetDataContext(object context)
    {
        treeGrid.DataContext = context;

        treeView.LayoutUpdated += (sender, e) =>
        {
            // return when empty
            if (treeView.Items.Count == 0)
                return;

            // return when there are more than one root nodes
            if (treeView.Items.Count > 1)
                return;

            var root = (TreeViewItem)treeView.ItemContainerGenerator.ContainerFromItem(treeView.Items[0]);
            if (root == null)
                return;

            root.IsExpanded = true;
        };
    }
}
