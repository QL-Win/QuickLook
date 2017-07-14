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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QuickLook.Annotations;
using QuickLook.Helpers;
using QuickLook.Helpers.BlurLibrary;
using QuickLook.Plugin;

namespace QuickLook
{
    /// <summary>
    ///     Interaction logic for MainWindowTransparent.xaml
    /// </summary>
    public partial class MainWindowTransparent : Window, INotifyPropertyChanged
    {
        private bool _pinned;

        internal MainWindowTransparent()
        {
            // this object should be initialized before loading UI components, because many of which are binding to it.
            ContextObject = new ContextObject();

            InitializeComponent();

            FontFamily = new FontFamily(TranslationHelper.GetString("UI_FontFamily", failsafe: "Segoe UI"));

            SourceInitialized += (sender, e) =>
            {
                if (AllowsTransparency)
                    BlurWindow.EnableWindowBlur(this);
            };

            buttonPin.MouseLeftButtonUp += (sender, e) =>
            {
                if (Pinned) return;
                Pinned = true;
                buttonOpenWith.Visibility = Visibility.Collapsed;
                ViewWindowManager.GetInstance().ForgetCurrentWindow();
            };

            buttonCloseWindow.MouseLeftButtonUp += (sender, e) =>
            {
                if (Pinned)
                    BeginClose();
                else
                    ViewWindowManager.GetInstance().ClosePreview();
            };

            buttonOpenWith.Click += (sender, e) =>
                ViewWindowManager.GetInstance().RunAndClosePreview();
        }

        public bool Pinned
        {
            get => _pinned;
            private set
            {
                _pinned = value;
                OnPropertyChanged();
            }
        }

        public string PreviewPath { get; private set; }
        public IViewer Plugin { get; private set; }

        public ContextObject ContextObject { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void RunAndHide()
        {
            if (string.IsNullOrEmpty(PreviewPath))
                return;

            try
            {
                Process.Start(new ProcessStartInfo(PreviewPath)
                {
                    WorkingDirectory = Path.GetDirectoryName(PreviewPath)
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
                Dispatcher.BeginInvoke(new Action(this.BringToFront), DispatcherPriority.Render);

                return;
            }

            // is the window is now now maximized, do not move it
            if (WindowState == WindowState.Maximized)
                return;

            // if this is a new window, place it to top
            if (Visibility != Visibility.Visible)
                this.BringToFront();

            var screen = WindowHelper.GetCurrentWindowRect();

            // if the window is visible, place new window in respect to the old center point.
            // otherwise, place it to the screen center.
            var oldCenterX = Visibility == Visibility.Visible ? Left + Width / 2 : screen.Left + screen.Width / 2;
            var oldCenterY = Visibility == Visibility.Visible ? Top + Height / 2 : screen.Top + screen.Height / 2;

            var newLeft = oldCenterX - size.Width / 2;
            var newTop = oldCenterY - size.Height / 2;

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

            Plugin?.Cleanup();
            Plugin = null;

            ProcessHelper.PerformAggressiveGC();
        }

        internal void BeginShow(IViewer matchedPlugin, string path, Action<ExceptionDispatchInfo> exceptionHandler)
        {
            PreviewPath = path;
            Plugin = matchedPlugin;

            ContextObject.ViewerWindow = this;

            // get window size before showing it
            Plugin.Prepare(path, ContextObject);

            SetOpenWithButtonAndPath();

            // revert UI changes
            ContextObject.IsBusy = true;

            var newHeight = ContextObject.PreferredSize.Height + titlebar.Height + windowBorder.BorderThickness.Top +
                            windowBorder.BorderThickness.Bottom;
            var newWidth = ContextObject.PreferredSize.Width + windowBorder.BorderThickness.Left +
                           windowBorder.BorderThickness.Right;

            ResizeAndCenter(new Size(newWidth, newHeight));

            chrome.CaptionHeight = ContextObject.FullWindowDragging ? Height : titlebar.Height - 5;

            if (Visibility != Visibility.Visible)
                Show();

            //WindowHelper.SetActivate(new WindowInteropHelper(this), ContextObject.CanFocus);

            // load plugin, do not block UI
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        Plugin.View(path, ContextObject);
                    }
                    catch (Exception e)
                    {
                        exceptionHandler(ExceptionDispatchInfo.Capture(e));
                    }
                }),
                DispatcherPriority.Input);
        }

        private void SetOpenWithButtonAndPath()
        {
            var isExe = FileHelper.GetAssocApplication(PreviewPath, out string appFriendlyName);

            buttonOpenWith.Content = isExe == null
                ? Directory.Exists(PreviewPath)
                    ? string.Format(TranslationHelper.GetString("MW_BrowseFolder"), Path.GetFileName(PreviewPath))
                    : string.Format(TranslationHelper.GetString("MW_Open"), Path.GetFileName(PreviewPath))
                : isExe == true
                    ? string.Format(TranslationHelper.GetString("MW_Run"), appFriendlyName)
                    : string.Format(TranslationHelper.GetString("MW_OpenWith"), appFriendlyName);
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

        internal void BeginClose()
        {
            UnloadPlugin();

            Close();

            ProcessHelper.PerformAggressiveGC();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}