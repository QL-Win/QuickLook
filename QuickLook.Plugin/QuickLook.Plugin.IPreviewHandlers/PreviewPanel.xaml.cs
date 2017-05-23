using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace QuickLook.Plugin.IPreviewHandlers
{
    /// <summary>
    ///     Interaction logic for PreviewPanel.xaml
    /// </summary>
    public partial class PreviewPanel : UserControl, IDisposable
    {
        private PreviewHandlerHost _control;

        public PreviewPanel()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                presenter.Child = null;
                presenter?.Dispose();

                _control?.Dispose();
                _control = null;
            }));
        }

        public void PreviewFile(string file, ContextObject context)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                _control = new PreviewHandlerHost();
                presenter.Child = _control;
                _control.Open(file);
            }), DispatcherPriority.Render);

            SetForegroundWindow(new WindowInteropHelper(context.ViewerWindow).Handle);
            SetActiveWindow(presenter.Handle);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetActiveWindow(IntPtr hWnd);
    }
}