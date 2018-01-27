// Copyright © 2017 Paddy Xu
// 
// This file is part of QuickLook program.
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using QuickLook.Common;

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
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                presenter.Child = null;
                presenter?.Dispose();

                _control?.Dispose();
                _control = null;
            }));
        }

        public void PreviewFile(string file, ContextObject context)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _control = new PreviewHandlerHost();
                presenter.Child = _control;
                _control.Open(file);
            }), DispatcherPriority.Loaded);

            //SetForegroundWindow(new WindowInteropHelper(context.ViewerWindow).Handle);
            //SetActiveWindow(presenter.Handle);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetActiveWindow(IntPtr hWnd);
    }
}