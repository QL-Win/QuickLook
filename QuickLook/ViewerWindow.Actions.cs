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
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Helpers;

namespace QuickLook
{
    public partial class ViewerWindow
    {
        internal void Run()
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

        private void ResizeAndCentre(Size size)
        {
            // if the window is now now maximized, do not move it
            if (WindowState == WindowState.Maximized)
                return;

            var newRect = IsLoaded ? ResizeAndCentreExistingWindow(size) : ResizeAndCentreNewWindow(size);

            if (IsLoaded)
            {
                this.MoveWindow(newRect.Left, newRect.Top, newRect.Width, newRect.Height);
            }
            else
            {
                Top = newRect.Top;
                Left = newRect.Left;
                Width = newRect.Width;
                Height = newRect.Height;
            }
        }

        private Rect ResizeAndCentreExistingWindow(Size size)
        {
            // align window just like in macOS ...
            // 
            // |10%|    80%    |10%|
            // |---|-----------|---|---
            // |TL |     T     |TR |10%
            // |---|-----------|---|---
            // |   |           |   |
            // |L  |     C     | R |80%
            // |   |           |   |
            // |---|-----------|---|---
            // |LB |     B     |RB |10%
            // |---|-----------|---|---

            const double limitPercentX = 0.1;
            const double limitPercentY = 0.1;

            var oldRect = new Rect(Left, Top, Width, Height);

            // scale to new size, maintain centre
            var newRect = Rect.Inflate(oldRect,
                (Math.Max(MinWidth, size.Width) - oldRect.Width) / 2,
                (Math.Max(MinHeight, size.Height) - oldRect.Height) / 2);

            var desktopRect = WindowHelper.GetDesktopRectFromWindow(this);

            var leftLimit = desktopRect.Left + desktopRect.Width * limitPercentX;
            var rightLimit = desktopRect.Right - desktopRect.Width * limitPercentX;
            var topLimit = desktopRect.Top + desktopRect.Height * limitPercentY;
            var bottomLimit = desktopRect.Bottom - desktopRect.Height * limitPercentY;

            if (oldRect.Left < leftLimit && oldRect.Right < rightLimit) // L
                newRect.Location = new Point(Math.Max(oldRect.Left, desktopRect.Left), newRect.Top);
            else if (oldRect.Left > leftLimit && oldRect.Right > rightLimit) // R
                newRect.Location = new Point(Math.Min(oldRect.Right, desktopRect.Right) - newRect.Width, newRect.Top);
            else // C, fix window boundary
                newRect.Offset(
                    Math.Max(0, desktopRect.Left - newRect.Left) + Math.Min(0, desktopRect.Right - newRect.Right), 0);

            if (oldRect.Top < topLimit && oldRect.Bottom < bottomLimit) // T
                newRect.Location = new Point(newRect.Left, Math.Max(oldRect.Top, desktopRect.Top));
            else if (oldRect.Top > topLimit && oldRect.Bottom > bottomLimit) // B
                newRect.Location = new Point(newRect.Left,
                    Math.Min(oldRect.Bottom, desktopRect.Bottom) - newRect.Height);
            else // C, fix window boundary
                newRect.Offset(0,
                    Math.Max(0, desktopRect.Top - newRect.Top) + Math.Min(0, desktopRect.Bottom - newRect.Bottom));

            return newRect;
        }

        private Rect ResizeAndCentreNewWindow(Size size)
        {
            var desktopRect = WindowHelper.GetCurrentDesktopRect();

            var newRect = Rect.Inflate(desktopRect,
                (Math.Max(MinWidth, size.Width) - desktopRect.Width) / 2,
                (Math.Max(MinHeight, size.Height) - desktopRect.Height) / 2);

            return newRect;
        }

        internal void UnloadPlugin()
        {
            // the focused element will not processed by GC: https://stackoverflow.com/questions/30848939/memory-leak-due-to-window-efectivevalues-retention
            FocusManager.SetFocusedElement(this, null);
            Keyboard.DefaultRestoreFocusMode =
                RestoreFocusMode.None; // WPF will put the focused item into a "_restoreFocus" list ... omg
            Keyboard.ClearFocus();

            _canOldPluginResize = ContextObject.CanResize;

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

            _path = string.Empty;
        }

        internal void BeginShow(IViewer matchedPlugin, string path,
            Action<string, ExceptionDispatchInfo> exceptionHandler)
        {
            _path = path;
            Plugin = matchedPlugin;

            // get window size before showing it
            try
            {
                Plugin.Prepare(path, ContextObject);
            }
            catch (Exception e)
            {
                exceptionHandler(path, ExceptionDispatchInfo.Capture(e));
                return;
            }

            SetOpenWithButtonAndPath();

            // revert UI changes
            ContextObject.IsBusy = true;

            var newHeight = ContextObject.PreferredSize.Height + BorderThickness.Top + BorderThickness.Bottom +
                            (ContextObject.TitlebarOverlap ? 0 : windowCaptionContainer.Height);
            var newWidth = ContextObject.PreferredSize.Width + BorderThickness.Left + BorderThickness.Right;

            var newSize = new Size(newWidth, newHeight);
            // if use has adjusted the window size, keep it
            if (_customWindowSize != Size.Empty)
                newSize = _customWindowSize;
            else
                _ignoreNextWindowSizeChange = true;

            ResizeAndCentre(newSize);
            Dispatcher.BeginInvoke(new Action(() => this.BringToFront(Topmost)), DispatcherPriority.Render);

            if (Visibility != Visibility.Visible)
                Show();

            //ShowWindowCaptionContainer(null, null);
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
                        exceptionHandler(path, ExceptionDispatchInfo.Capture(e));
                    }
                }),
                DispatcherPriority.Input);
        }

        private void SetOpenWithButtonAndPath()
        {
            // share icon
            buttonShare.Visibility = ShareHelper.IsShareSupported(_path) ? Visibility.Visible : Visibility.Collapsed;

            // open icon
            if (Directory.Exists(_path))
            {
                buttonOpen.ToolTip = string.Format(TranslationHelper.Get("MW_BrowseFolder"), Path.GetFileName(_path));
                return;
            }

            var isExe = FileHelper.IsExecutable(_path, out var appFriendlyName);
            if (isExe)
            {
                buttonOpen.ToolTip = string.Format(TranslationHelper.Get("MW_Run"), appFriendlyName);
                return;
            }

            // not an exe
            var found = FileHelper.GetAssocApplication(_path, out appFriendlyName);
            if (found)
            {
                buttonOpen.ToolTip = string.Format(TranslationHelper.Get("MW_OpenWith"), appFriendlyName);
                return;
            }

            // assoc not found
            buttonOpen.ToolTip = string.Format(TranslationHelper.Get("MW_Open"), Path.GetFileName(_path));
        }

        internal void BeginHide()
        {
            // reset custom window size
            _customWindowSize = Size.Empty;
            _ignoreNextWindowSizeChange = true;

            UnloadPlugin();

            // if the this window is hidden in Max state, new show() will results in failure:
            // "Cannot show Window when ShowActivated is false and WindowState is set to Maximized"
            //WindowState = WindowState.Normal;

            Hide();
            //Dispatcher.BeginInvoke(new Action(Hide), DispatcherPriority.ApplicationIdle);

            ViewWindowManager.GetInstance().ForgetCurrentWindow();
            BeginClose();

            ProcessHelper.PerformAggressiveGC();
        }

        internal void BeginClose()
        {
            UnloadPlugin();
            busyDecorator.Dispose();

            Close();

            ProcessHelper.PerformAggressiveGC();
        }
    }
}