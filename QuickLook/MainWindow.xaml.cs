using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using QuickLook.Helpers;
using QuickLook.Plugin;

namespace QuickLook
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow : Window, IDisposable
    {
        internal MainWindow()
        {
            // this object should be initialized before loading UI components, because many of which are binding to it.
            ContextObject = new ContextObject();

            InitializeComponent();

            // do not set TopMost property if we are now debugging. it makes debugging painful...
            if (!Debugger.IsAttached)
                Topmost = true;

            // restore changes by Designer
            windowPanel.Opacity = 0d;
            busyIndicatorLayer.Visibility = Visibility.Visible;

            Loaded += (sender, e) => AeroGlassHelper.EnableBlur(this);

            buttonCloseWindow.MouseLeftButtonUp += (sender, e) => Close();
            titlebarTitleArea.MouseLeftButtonDown += DragMoveCurrentWindow;
        }

        public ContextObject ContextObject { get; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            ContextObject?.Dispose();

            // stop the background thread
            busyDecorator?.Dispose();
        }

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
            Height = ContextObject.PreferredSize.Height + titlebar.Height + windowBorder.BorderThickness.Top +
                     windowBorder.BorderThickness.Bottom;
            Width = ContextObject.PreferredSize.Width + windowBorder.BorderThickness.Left +
                    windowBorder.BorderThickness.Right;

            ResizeMode = ContextObject.CanResize ? ResizeMode.CanResizeWithGrip : ResizeMode.NoResize;

            base.Show();

            WindowHelper.SetNoactivate(new WindowInteropHelper(this));
        }

        internal void BeginShow(IViewer matchedPlugin, string path)
        {
            ContextObject.CurrentContentContainer = viewContentContainer;
            ContextObject.ViewerPlugin = matchedPlugin;

            // get window size before showing it
            matchedPlugin.BoundViewSize(path, ContextObject);

            Show();

            matchedPlugin.View(path, ContextObject);
        }

        ~MainWindow()
        {
            Dispose();
        }
    }
}