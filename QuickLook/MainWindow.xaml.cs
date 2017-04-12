using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using QuickLook.ExtensionMethods;
using QuickLook.Plugin;
using QuickLook.Utilities;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace QuickLook
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window
    {
        internal MainWindow()
        {
            InitializeComponent();

            WindowContainer.Width = LoadingIcon.Width;
            WindowContainer.Height = LoadingIcon.Height;
            ContentContainer.Visibility = Visibility.Hidden;
        }
        
        private void ZoomToPreferedSize()
        {
            Storyboard sb = new Storyboard { };
            ParallelTimeline ptl = new ParallelTimeline();

            DoubleAnimation animationWidth = new DoubleAnimation
            {
                From = WindowContainer.Width,
                To = ContentContainer.Width,
                Duration = TimeSpan.FromSeconds(0.2),
                DecelerationRatio = 0.3
            };

            DoubleAnimation animationHeight = new DoubleAnimation
            {
                From = WindowContainer.Height,
                To = ContentContainer.Height,
                Duration = TimeSpan.FromSeconds(0.2),
                DecelerationRatio = 0.3
            };

            Storyboard.SetTarget(animationWidth, WindowContainer);
            Storyboard.SetTarget(animationHeight, WindowContainer);
            Storyboard.SetTargetProperty(animationWidth, new PropertyPath(WidthProperty));
            Storyboard.SetTargetProperty(animationHeight, new PropertyPath(HeightProperty));

            ptl.Children.Add(animationWidth);
            ptl.Children.Add(animationHeight);

            sb.Children.Add(ptl);

            sb.Begin();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ZoomToPreferedSize();
        }
    }
}
