using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using QuickLook.Helpers;
using QuickLook.Helpers.BlurLibrary;
using QuickLook.Plugin;

namespace QuickLook
{
    /// <summary>
    ///     Interaction logic for MainWindowTransparent.xaml
    /// </summary>
    public partial class MainWindowTransparent : Window
    {
        internal MainWindowTransparent()
        {
            // this object should be initialized before loading UI components, because many of which are binding to it.
            ContextObject = new ContextObject();

            InitializeComponent();

            // do not set TopMost property if we are now debugging. it makes debugging painful...
            if (!Debugger.IsAttached)
                Topmost = true;

            SourceInitialized += (sender, e) =>
            {
                if (AllowsTransparency)
                    BlurWindow.EnableWindowBlur(this);
            };

            buttonCloseWindow.MouseLeftButtonUp += (sender, e) => Hide();
        }

        public ContextObject ContextObject { get; private set; }

        private new void Show()
        {
            // revert UI changes
            ContextObject.IsBusy = true;

            var newHeight = ContextObject.PreferredSize.Height + titlebar.Height + windowBorder.BorderThickness.Top +
                            windowBorder.BorderThickness.Bottom;
            var newWidth = ContextObject.PreferredSize.Width + windowBorder.BorderThickness.Left +
                           windowBorder.BorderThickness.Right;

            ResizeAndCenter(new Size(newWidth, newHeight));

            base.Show();

            //if (!ContextObject.CanFocus)
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

        internal void UnloadPlugin()
        {
            container.Content = null;

            // clean up plugin and refresh ContextObject for next use
            ContextObject.ViewerPlugin?.Cleanup();
        }

        private new void Hide()
        {
            if (App.RunningAsViewer)
                Application.Current.Shutdown();

            UnloadPlugin();
            ContextObject.Reset();

            GC.Collect();

            // revert UI changes
            ContextObject.IsBusy = true;

            base.Hide();
            //Left -= 10000;
            //Dispatcher.Delay(100, _ => base.Hide());
        }

        internal void BeginShow(IViewer matchedPlugin, string path)
        {
            ContextObject.CurrentContentContainer = container;
            ContextObject.ViewerPlugin = matchedPlugin;
            ContextObject.ViewerWindow = this;

            // get window size before showing it
            ContextObject.ViewerPlugin.Prepare(path, ContextObject);

            Show();

            // load plugin, do not block UI
            Exception thrown = null;
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        ContextObject.ViewerPlugin.View(path, ContextObject);
                    }
                    catch (Exception e)
                    {
                        thrown = e;
                    }
                }),
                DispatcherPriority.Render).Wait();

            if (thrown != null)
                throw thrown;
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