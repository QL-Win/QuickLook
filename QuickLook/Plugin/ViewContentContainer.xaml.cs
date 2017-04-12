using System.Windows.Controls;

namespace QuickLook.Plugin
{
    /// <summary>
    ///     Interaction logic for ViewContentContainer.xaml
    /// </summary>
    public partial class ViewContentContainer : UserControl
    {
        public ViewContentContainer()
        {
            InitializeComponent();
        }

        public void SetContent(object content)
        {
            Container.Content = content;
        }
    }
}