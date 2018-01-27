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
using System.Windows.Media;
using System.Windows.Media.Animation;
using QuickLook.Helpers;
using QuickLook.Plugin;

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

            InitializeComponent();

            FontFamily = new FontFamily(TranslationHelper.Get("UI_FontFamily", failsafe: "Segoe UI"));

            SizeChanged += SaveWindowSizeOnSizeChanged;

            StateChanged += (sender, e) => _ignoreNextWindowSizeChange = true;

            windowFrameContainer.PreviewMouseMove += ShowWindowCaptionContainer;

            buttonTop.Click += (sender, e) =>
            {
                Topmost = !Topmost;
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

            buttonOpenWith.Click += (sender, e) =>
            {
                if (Pinned)
                    RunAndClose();
                else
                    ViewWindowManager.GetInstance().RunAndClosePreview();
            };

            buttonWindowStatus.Click += (sender, e) =>
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

            buttonShare.Click += Share;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (SystemParameters.IsGlassEnabled && App.IsWin10)
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