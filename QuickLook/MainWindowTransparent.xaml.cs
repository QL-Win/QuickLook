using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Input;
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
        private string _path = string.Empty;
        private IViewer _plugin;

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

            buttonCloseWindow.MouseLeftButtonUp += (sender, e) =>
                ViewWindowManager.GetInstance().ClosePreview();

            buttonOpenWith.Click += (sender, e) =>
                ViewWindowManager.GetInstance().RunAndClosePreview();
        }

        public ContextObject ContextObject { get; private set; }

        internal void RunAndHide()
        {
            if (string.IsNullOrEmpty(_path))
                return;

            try
            {
                Process.Start(new ProcessStartInfo(_path)
                {
                    WorkingDirectory = Path.GetDirectoryName(_path)
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            BeginHide();
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
            // the focused element will not processed by GC: https://stackoverflow.com/questions/30848939/memory-leak-due-to-window-efectivevalues-retention
            FocusManager.SetFocusedElement(this, null);
            Keyboard.DefaultRestoreFocusMode =
                RestoreFocusMode.None; // WPF will put the focused item into a "_restoreFocus" list ... omg
            Keyboard.ClearFocus();

            ContextObject.Reset();

            _plugin?.Cleanup();
            _plugin = null;

            ProcessHelper.PerformAggressiveGC();
        }

        internal void BeginShow(IViewer matchedPlugin, string path)
        {
            _path = path;
            _plugin = matchedPlugin;

            ContextObject.ViewerWindow = this;

            // get window size before showing it
            _plugin.Prepare(path, ContextObject);

            SetOpenWithButtonAndPath();

            // revert UI changes
            ContextObject.IsBusy = true;

            var newHeight = ContextObject.PreferredSize.Height + titlebar.Height + windowBorder.BorderThickness.Top +
                            windowBorder.BorderThickness.Bottom;
            var newWidth = ContextObject.PreferredSize.Width + windowBorder.BorderThickness.Left +
                           windowBorder.BorderThickness.Right;

            ResizeAndCenter(new Size(newWidth, newHeight));

            Show();

            //WindowHelper.SetActivate(new WindowInteropHelper(this), ContextObject.CanFocus);

            // load plugin, do not block UI
            Exception thrown = null;
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        _plugin.View(path, ContextObject);
                    }
                    catch (Exception e)
                    {
                        thrown = e;
                    }
                }),
                DispatcherPriority.Render).Wait();

            if (thrown != null)
                ExceptionDispatchInfo.Capture(thrown).Throw();
        }

        private void SetOpenWithButtonAndPath()
        {
            var isExe = FileHelper.GetAssocApplication(_path, out string appFriendlyName);

            buttonOpenWith.Content = isExe == null
                ? Directory.Exists(_path)
                    ? $"Browse “{Path.GetFileName(_path)}”"
                    : "Open..."
                : isExe == true
                    ? $"Run “{appFriendlyName}”"
                    : $"Open with “{appFriendlyName}”";
        }

        internal void BeginHide()
        {
            UnloadPlugin();

            // if the this window is hidden in Max state, new show() will results in failure:
            // "Cannot show Window when ShowActivated is false and WindowState is set to Maximized"
            WindowState = WindowState.Normal;

            Hide();

            ProcessHelper.PerformAggressiveGC();
        }
    }
}