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
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using QuickLook.Common.ExtensionMethods;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Helpers;

namespace QuickLook
{
    /// <summary>
    ///     Interaction logic for ViewerWindow.xaml
    /// </summary>
    public partial class ViewerWindow : Window
    {
        private Size _customWindowSize = Size.Empty;
        private bool _ignoreNextWindowSizeChange;
        private string _path = string.Empty;

        internal ViewerWindow()
        {
            // this object should be initialized before loading UI components, because many of which are binding to it.
            ContextObject = new ContextObject();

            ContextObject.PropertyChanged += ContextObject_PropertyChanged;

            InitializeComponent();

            Icon = (App.IsWin10 ? Properties.Resources.app_white_png : Properties.Resources.app_png).ToBitmapSource();

            FontFamily = new FontFamily(TranslationHelper.Get("UI_FontFamily", failsafe: "Segoe UI"));

            SizeChanged += SaveWindowSizeOnSizeChanged;

            StateChanged += (sender, e) => _ignoreNextWindowSizeChange = true;

            // bring the window to top when users click in the client area.
            // the non-client area is handled by the WndProc inside OnSourceInitialized().
            // This is buggy for Windows 7 and 8: https://github.com/QL-Win/QuickLook/issues/644#issuecomment-628921704
            if (App.IsWin10)
                PreviewMouseDown += (sender, e) => this.BringToFront(false);

            windowFrameContainer.PreviewMouseMove += ShowWindowCaptionContainer;

            Topmost = SettingHelper.Get("Topmost", false);
            buttonTop.Tag = Topmost ? "Top" : "Auto";

            buttonTop.Click += (sender, e) =>
            {
                Topmost = !Topmost;
                SettingHelper.Set("Topmost", Topmost);
                buttonTop.Tag = Topmost ? "Top" : "Auto";
            };

            buttonPin.Click += (sender, e) =>
            {
                if (Pinned)
                    return;

                ViewWindowManager.GetInstance().ForgetCurrentWindow();
            };

            buttonCloseWindow.Click += (sender, e) =>
            {
                if (Pinned)
                    BeginClose();
                else
                    ViewWindowManager.GetInstance().ClosePreview();
            };

            buttonOpen.Click += (sender, e) =>
            {
                if (Pinned)
                    RunAndClose();
                else
                    ViewWindowManager.GetInstance().RunAndClosePreview();
            };

            buttonWindowStatus.Click += (sender, e) =>
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

            buttonShare.Click += (sender, e) => ShareHelper.Share(_path, this);
            buttonOpenWith.Click += (sender, e) => ShareHelper.Share(_path, this, true);
        }

        // bring the window to top when users click in the non-client area.
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // The non-focusable trick is buggy for Windows 7 and 8
            // https://github.com/QL-Win/QuickLook/issues/644#issuecomment-628921704
            if (App.IsWin10)
            {
                this.SetNoactivate();

                HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(
                    (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
                    {
                        switch (msg)
                        {
                            case 0x0112: // WM_SYSCOMMAND
                                this.BringToFront(false);
                                break;
                        }

                        return IntPtr.Zero;
                    });
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (SystemParameters.IsGlassEnabled && App.IsWin10 && !App.IsGPUInBlacklist)
                WindowHelper.EnableBlur(this);
            else
                Background = (Brush) FindResource("MainWindowBackgroundNoTransparent");
        }

        private void SaveWindowSizeOnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // first shown?
            if (e.PreviousSize == new Size(0, 0))
                return;
            // resize when switching preview?
            if (_ignoreNextWindowSizeChange)
            {
                _ignoreNextWindowSizeChange = false;
                return;
            }

            // by user?
            _customWindowSize = new Size(Width, Height);
        }

        private void ShowWindowCaptionContainer(object sender, MouseEventArgs e)
        {
            var show = (Storyboard) windowCaptionContainer.FindResource("ShowCaptionContainerStoryboard");

            if (windowCaptionContainer.Opacity == 0 || windowCaptionContainer.Opacity == 1)
                show.Begin();
        }

        private void AutoHideCaptionContainer(object sender, EventArgs e)
        {
            if (!ContextObject.TitlebarAutoHide)
                return;

            if (!ContextObject.TitlebarOverlap)
                return;

            if (windowCaptionContainer.IsMouseOver)
                return;

            var hide = (Storyboard) windowCaptionContainer.FindResource("HideCaptionContainerStoryboard");

            hide.Begin();
        }
    }
}