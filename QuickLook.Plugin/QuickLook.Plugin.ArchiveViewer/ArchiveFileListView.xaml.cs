using System;
using System.Windows.Controls;

namespace QuickLook.Plugin.ArchiveViewer
{
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

        ~ArchiveFileListView()
        {
            Dispose();
        }

        public void SetDataContext(object context)
        {
            treeGrid.DataContext = context;

            treeView.Loaded += (sender, e) =>
            {
                // return when empty
                if (treeView.Items.Count == 0)
                    return;

                // return when there are more than one root nodes
                if (treeView.Items.Count > 1)
                    return;

                var root = (TreeViewItem) treeView.ItemContainerGenerator.ContainerFromItem(treeView.Items[0]);
                if (root == null)
                    return;

                root.IsExpanded = true;
            };
        }
    }
}