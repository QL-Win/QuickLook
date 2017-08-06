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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using QuickLook.Annotations;
using QuickLook.Controls;
using QuickLook.Helpers;
using QuickLook.Plugin;

namespace QuickLook
{
    /// <summary>
    ///     Interaction logic for MainWindowTransparent.xaml
    /// </summary>
    public partial class MainWindowTransparent : MainWindowBase, INotifyPropertyChanged
    {
        private bool _pinned;
        private bool _restoreForDragMove;

        internal MainWindowTransparent()
        {
            // this object should be initialized before loading UI components, because many of which are binding to it.
            ContextObject = new ContextObject();

            InitializeComponent();

            FontFamily = new FontFamily(TranslationHelper.GetString("UI_FontFamily", failsafe: "Segoe UI"));

            windowCaptionContainer.MouseLeftButtonDown += WindowDragMoveStart;
            windowCaptionContainer.MouseMove += WindowDragMoving;
            windowCaptionContainer.MouseLeftButtonUp += WindowDragMoveEnd;

            buttonPin.Click += (sender, e) =>
            {
                if (Pinned)
                {
                    buttonCloseWindow.RaiseEvent(
                        new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                        {
                            RoutedEvent = MouseLeftButtonUpEvent
                        });
                    return;
                }
                Pinned = true;
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

            buttonShare.Click +=
                (sender, e) => RunWith("rundll32.exe", $"shell32.dll,OpenAs_RunDLL {Path}");
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

        public string Path { get; private set; }
        public IViewer Plugin { get; private set; }

        public ContextObject ContextObject { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void WindowDragMoveEnd(object sender, MouseButtonEventArgs e)
        {
            _restoreForDragMove = false;
        }

        private void WindowDragMoving(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;
            if (!_restoreForDragMove)
                return;
            _restoreForDragMove = false;

            var scale = DpiHelper.GetCurrentScaleFactor();
            var point = PointToScreen(e.MouseDevice.GetPosition(this));
            point.X /= scale.Horizontal;
            point.Y /= scale.Vertical;

            var monitor = WindowHelper.GetCurrentWindowRect();
            var precentLeft = (point.X - monitor.Left) / monitor.Width;
            var precentTop = (point.Y - monitor.Top) / monitor.Height;

            Left = point.X - RestoreBounds.Width * precentLeft;
            Top = point.Y - RestoreBounds.Height * precentTop;

            WindowState = WindowState.Normal;
            
            DragMove();
        }

        private void WindowDragMoveStart(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (ResizeMode != ResizeMode.CanResize &&
                    ResizeMode != ResizeMode.CanResizeWithGrip)
                    return;

                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                _restoreForDragMove = WindowState == WindowState.Maximized;
                DragMove();
            }
        }

        internal void RunWith(string with, string arg)
        {
            if (string.IsNullOrEmpty(Path))
                return;

            try
            {
                Process.Start(new ProcessStartInfo(with)
                {
                    Arguments = arg,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(Path)
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        internal void Run()
        {
            if (string.IsNullOrEmpty(Path))
                return;

            try
            {
                Process.Start(new ProcessStartInfo(Path)
                {
                    WorkingDirectory = System.IO.Path.GetDirectoryName(Path)
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        internal void RunAndHide()
        {
            Run();
            BeginHide();
        }

        internal void RunAndClose()
        {
            Run();
            BeginClose();
        }

        private void ResizeAndCenter(Size size)
        {
            // resize to MinSize first
            size.Width = Math.Max(size.Width, MinWidth);
            size.Height = Math.Max(size.Height, MinHeight);

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

            try
            {
                Plugin?.Cleanup();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            Plugin = null;

            Path = string.Empty;
        }

        internal void BeginShow(IViewer matchedPlugin, string path, Action<ExceptionDispatchInfo> exceptionHandler)
        {
            Path = path;
            Plugin = matchedPlugin;

            ContextObject.ViewerWindow = this;

            // get window size before showing it
            Plugin.Prepare(path, ContextObject);

            SetOpenWithButtonAndPath();

            // revert UI changes
            ContextObject.IsBusy = true;

            var margin = windowFrameContainer.Margin.Top * 2;

            var newHeight = ContextObject.PreferredSize.Height + margin +
                            (ContextObject.TitlebarOverlap ? 0 : windowCaptionContainer.Height);
            var newWidth = ContextObject.PreferredSize.Width + margin;

            ResizeAndCenter(new Size(newWidth, newHeight));

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
            buttonOpenWithText.Inlines.Clear();

            if (Directory.Exists(Path))
            {
                AddToInlines("MW_BrowseFolder", System.IO.Path.GetFileName(Path));
                return;
            }
            var isExe = FileHelper.IsExecutable(Path, out string appFriendlyName);
            if (isExe)
            {
                AddToInlines("MW_Run", appFriendlyName);
                return;
            }
            // not an exe
            var found = FileHelper.GetAssocApplication(Path, out appFriendlyName);
            if (found)
            {
                AddToInlines("MW_OpenWith", appFriendlyName);
                return;
            }
            // assoc not found
            AddToInlines("MW_Open", System.IO.Path.GetFileName(Path));

            void AddToInlines(string str, string replaceWith)
            {
                str = TranslationHelper.GetString(str);
                var elements = str.Split(new[] {"{0}"}, StringSplitOptions.None).ToList();
                while (elements.Count < 2)
                    elements.Add(string.Empty);

                buttonOpenWithText.Inlines.Add(
                    new Run(elements[0]) {FontWeight = FontWeights.Normal}); // text beforehand
                buttonOpenWithText.Inlines.Add(
                    new Run(replaceWith) {FontWeight = FontWeights.SemiBold}); // appFriendlyName
                buttonOpenWithText.Inlines.Add(
                    new Run(elements[1]) {FontWeight = FontWeights.Normal}); // text afterward
            }
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