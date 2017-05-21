using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;

namespace QuickLook.Plugin.IPreviewHandlers
{
    /// <summary>
    ///     Interaction logic for PreviewPanel.xaml
    /// </summary>
    public partial class PreviewPanel : UserControl, IDisposable
    {
        private PreviewHandlerHost _control = new PreviewHandlerHost();

        public PreviewPanel()
        {
            InitializeComponent();
        }

        public void Dispose()
        {
            presenter.Child = null;
            presenter?.Dispose();

            _control?.Dispose();
            _control = null;
        }

        public void PreviewFile(string file)
        {
            _control = new PreviewHandlerHost();

            presenter.Child = _control;

            _control.Open(file);

            SetActiveWindow(presenter.Handle);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetActiveWindow(IntPtr hWnd);
    }
}