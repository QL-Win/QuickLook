using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using QuickLook.Annotations;
using QuickLook.ExtensionMethods;
using QuickLook.Plugin;
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
            titlebarTitleArea.MouseLeftButtonDown += (sender, e) => DragMove();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            viewContentContainer.ViewerPlugin.Close();
            viewContentContainer.ViewerPlugin = null;

            GC.Collect();
        }

        internal new void Show()
        {
            loadingIconLayer.Opacity = 1;

            Height = viewContentContainer.PreferedSize.Height + titlebar.Height + windowBorder.BorderThickness.Top +
                     windowBorder.BorderThickness.Bottom;
            Width = viewContentContainer.PreferedSize.Width + windowBorder.BorderThickness.Left +
                    windowBorder.BorderThickness.Right;

            ResizeMode = viewContentContainer.CanResize ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize;

            base.Show();

            WindowHelper.SetNoactivate(new WindowInteropHelper(this));
        }

        internal void BeginShow(IViewer matchedPlugin, string path)
        {
            viewContentContainer.ViewerPlugin = matchedPlugin;

            // get window size before showing it
            matchedPlugin.Prepare(path, viewContentContainer);

            Show();

            matchedPlugin.View(path, viewContentContainer);

            ShowFinishLoadingAnimation();
        }

        private void ShowFinishLoadingAnimation()
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

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}