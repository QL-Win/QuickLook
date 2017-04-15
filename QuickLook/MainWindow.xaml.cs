using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Channels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using QuickLook.Annotations;
using QuickLook.ExtensionMethods;
using QuickLook.Utilities;

namespace QuickLook
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window, INotifyPropertyChanged
    {
        internal MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            ContentRendered += (sender, e) => AeroGlass.EnableBlur(this);
            Closed += MainWindow_Closed;

            buttonCloseWindow.MouseLeftButtonUp += CloseCurrentWindow;
            titlebarTitleArea.MouseDown += (sender, e) => DragMove();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            viewContentContainer.ViewerPlugin.Close();
        }

        internal new void Show()
        {
            loadingIconLayer.Opacity = 1;

            Height = viewContentContainer.PreferedSize.Height + titlebar.Height;
            Width = viewContentContainer.PreferedSize.Width;

            base.Show();
        }

        internal void ShowFinishLoadingAnimation()
        {
            var speed = 100;

            var sb = new Storyboard();
            var ptl = new ParallelTimeline();
            
            var aOpacityR = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(speed)
            };
            
            Storyboard.SetTarget(aOpacityR, loadingIconLayer);
            Storyboard.SetTargetProperty(aOpacityR, new PropertyPath(OpacityProperty));
            
            ptl.Children.Add(aOpacityR);

            sb.Children.Add(ptl);

            sb.Begin();

            Dispatcher.DelayWithPriority(speed, o => loadingIconLayer.Visibility = Visibility.Hidden, null,
                DispatcherPriority.Render);
        }

        private void CloseCurrentWindow(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}