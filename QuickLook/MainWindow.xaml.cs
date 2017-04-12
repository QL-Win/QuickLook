using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using QuickLook.ExtensionMethods;

namespace QuickLook
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window
    {
        internal MainWindow()
        {
            InitializeComponent();

            WindowContainer.Width = LoadingIcon.Width;
            WindowContainer.Height = LoadingIcon.Height;
            ViewContentContainer.Opacity = 0;
        }

        internal new void Show()
        {
            Height = ViewContentContainer.Height;
            Width = ViewContentContainer.Width;

            base.Show();
        }

        internal void ShowFinishLoadingAnimation(TimeSpan delay = new TimeSpan())
        {
            var speed = 200;

            var sb = new Storyboard();
            var ptl = new ParallelTimeline {BeginTime = delay};

            var aWidth = new DoubleAnimation
            {
                From = WindowContainer.Width,
                To = ViewContentContainer.Width,
                Duration = TimeSpan.FromMilliseconds(speed),
                DecelerationRatio = 0.3
            };

            var aHeight = new DoubleAnimation
            {
                From = WindowContainer.Height,
                To = ViewContentContainer.Height,
                Duration = TimeSpan.FromMilliseconds(speed),
                DecelerationRatio = 0.3
            };

            var aOpacity = new DoubleAnimation
            {
                From = 0,
                To = 1,
                BeginTime = TimeSpan.FromMilliseconds(speed * 0.25),
                Duration = TimeSpan.FromMilliseconds(speed * 0.75)
            };

            var aOpacityR = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(speed * 2)
            };

            Storyboard.SetTarget(aWidth, WindowContainer);
            Storyboard.SetTarget(aHeight, WindowContainer);
            Storyboard.SetTarget(aOpacity, ViewContentContainer);
            Storyboard.SetTarget(aOpacityR, LoadingIconLayer);
            Storyboard.SetTargetProperty(aWidth, new PropertyPath(WidthProperty));
            Storyboard.SetTargetProperty(aHeight, new PropertyPath(HeightProperty));
            Storyboard.SetTargetProperty(aOpacity, new PropertyPath(OpacityProperty));
            Storyboard.SetTargetProperty(aOpacityR, new PropertyPath(OpacityProperty));

            ptl.Children.Add(aWidth);
            ptl.Children.Add(aHeight);
            ptl.Children.Add(aOpacity);
            ptl.Children.Add(aOpacityR);

            sb.Children.Add(ptl);

            sb.Begin();

            Dispatcher.DelayWithPriority(speed * 2, o => LoadingIconLayer.Visibility = Visibility.Hidden, null,
                DispatcherPriority.Render);
        }

        private void Close_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Close();

            // useless code to make everyone happy
            GC.Collect();
        }
    }
}