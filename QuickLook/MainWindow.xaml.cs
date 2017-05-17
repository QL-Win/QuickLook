using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using QuickLook.ExtensionMethods;
using QuickLook.Helpers;
using QuickLook.Helpers.BlurLibrary;
using QuickLook.Plugin;

namespace QuickLook
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window
    {
        internal MainWindow()
        {
            // this object should be initialized before loading UI components, because many of which are binding to it.
            ContextObject = new ContextObject();

            InitializeComponent();

            // do not set TopMost property if we are now debugging. it makes debugging painful...
            if (!Debugger.IsAttached)
                Topmost = true;

            Loaded += (sender, e) => BlurWindow.EnableWindowBlur(this);

            buttonCloseWindow.MouseLeftButtonUp += (sender, e) => { Hide(); };

            titleBarArea.PreviewMouseLeftButtonDown += DragMoveCurrentWindow;
        }

        public ContextObject ContextObject { get; private set; }

        private void DragMoveCurrentWindow(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                var dpi = DpiHelper.GetCurrentDpi();

                // MouseDevice.GetPosition() returns device-dependent coordinate, however WPF is not like that
                var point = PointToScreen(e.MouseDevice.GetPosition(this));

                Left = point.X / (dpi.HorizontalDpi / DpiHelper.DEFAULT_DPI) - RestoreBounds.Width * 0.5;
                Top = point.Y / (dpi.VerticalDpi / DpiHelper.DEFAULT_DPI);

                WindowState = WindowState.Normal;
            }

            DragMove();
        }

        private new void Show()
        {
            // revert UI changes
            ContextObject.IsBusy = true;

            var newHeight = ContextObject.PreferredSize.Height + titlebar.Height + windowBorder.BorderThickness.Top +
                            windowBorder.BorderThickness.Bottom;
            var newWidth = ContextObject.PreferredSize.Width + windowBorder.BorderThickness.Left +
                           windowBorder.BorderThickness.Right;

            ResizeAndCenter(new Size(newWidth, newHeight));

            ResizeMode = ContextObject.CanResize ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize;

            base.Show();

            //if (!ContextObject.Focusable)
            //    WindowHelper.SetNoactivate(new WindowInteropHelper(this));
        }

        private void ResizeAndCenter(Size size)
        {
            if (!IsLoaded)
            {
                // if the window is not loaded yet, just leave the problem to WPF
                Width = size.Width;
                Height = size.Height;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

                return;
            }

            // System.Windows.Forms does not consider DPI, so we need to do it maunally

            var screen = WindowHelper.GetCurrentWindowRect();

            var newLeft = screen.Left + (screen.Width - size.Width) / 2;
            var newTop = screen.Top + (screen.Height - size.Height) / 2;

            this.MoveWindow(newLeft, newTop, size.Width, size.Height);
        }

        private new void Hide()
        {
            if (App.RunningAsViewer)
                Application.Current.Shutdown();

            container.Content = null;

            // clean up plugin and refresh ContextObject for next use
            ContextObject.ViewerPlugin?.Cleanup();
            ContextObject.Reset();

            GC.Collect();

            // revert UI changes
            ContextObject.IsBusy = true;

            Left -= 10000;
            Dispatcher.Delay(100, _ => base.Hide());
        }

        internal void BeginShow(IViewer matchedPlugin, string path)
        {
            ContextObject.CurrentContentContainer = container;
            ContextObject.ViewerPlugin = matchedPlugin;

            // get window size before showing it
            matchedPlugin.Prepare(path, ContextObject);

            Show();

            // load plugin, do not block UI
            Dispatcher.BeginInvoke(new Action(() => matchedPlugin.View(path, ContextObject)),
                DispatcherPriority.Render);
        }

        internal bool BeginHide()
        {
            if (Visibility != Visibility.Visible)
                return false;

            Hide();

            return true;
        }
    }
}