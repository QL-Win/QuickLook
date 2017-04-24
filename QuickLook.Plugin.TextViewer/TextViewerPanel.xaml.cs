using System.IO;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Highlighting;

namespace QuickLook.Plugin.TextViewer
{
    /// <summary>
    ///     Interaction logic for TextViewerPanel.xaml
    /// </summary>
    public partial class TextViewerPanel : UserControl
    {
        public TextViewerPanel(string path)
        {
            InitializeComponent();

            LoadFile(path);
        }

        private void LoadFile(string path)
        {
            viewer.Load(path);

            viewer.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(path));
        }
    }
}