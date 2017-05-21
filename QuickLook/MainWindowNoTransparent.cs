using System.Windows.Media;

namespace QuickLook
{
    internal class MainWindowNoTransparent : MainWindowTransparent
    {
        public MainWindowNoTransparent()
        {
            Background = new SolidColorBrush(Colors.White);
            AllowsTransparency = false;
        }
    }
}