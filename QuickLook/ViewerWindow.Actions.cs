// Copyright © 2017 Paddy Xu and Frank Becker
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
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Helpers;

namespace QuickLook
{
    public partial class ViewerWindow
    {
        private static Rect _winRect = Rect.Empty;
        private bool _ignoreNextWindowLocationChange = false;

        public const int DefaultDpi = 96;

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

        internal void RunAndClose()
        {
            Run();
            Close();
        }

        private static System.Drawing.Rectangle ToDrawingRectangle(Rect r)
        {
            return new System.Drawing.Rectangle(
                (int)Math.Round(r.X),
                (int)Math.Round(r.Y),
                (int)Math.Round(r.Width),
                (int)Math.Round(r.Height));
        }

        private void PositionWindow(Size size)
        {
            // if the window is now now maximized, do not move it
            if (WindowState == WindowState.Maximized)
                return;

            size = new Size(Math.Max(MinWidth, size.Width), Math.Max(MinHeight, size.Height));

            var newRect = new Rect(new Point(0, 0), size);
            var useOldWindowPositioning = SettingHelper.Get("UseOldWindowPositioning", false);
            if (useOldWindowPositioning)
            {
                newRect = IsLoaded ? ResizeAndCentreExistingWindow(size) : ResizeAndCentreNewWindow(size);
            }
            else
            {
                if (_winRect.IsEmpty)
                {
                    newRect = ResizeAndCentreNewWindow(size);
                    _winRect = new Rect(newRect.Location, newRect.Size);
                }
                var scaledRect = ToDrawingRectangle(_winRect);

                // TODO: move to QL common?
                // When re-opening, try to make it fully visible on current screen.
                var screen = Screen.FromRectangle(scaledRect); // finds screen with largest portion of rect
                var workingArea = screen.WorkingArea;

                var dpiX = DefaultDpi;
                var dpiY = DefaultDpi;
                // hackier version to get hmonitor directly from screen:
                // GetDpiForMonitor((IntPtr)screen.GetHashCode(), MonitorDpiType.EFFECTIVE_DPI, out dpiX, out dpiY);
                var hmonitor = MonitorFromPoint(workingArea.Location, MONITOR_DEFAULTTONEAREST);
                GetDpiForMonitor(hmonitor, MonitorDpiType.EFFECTIVE_DPI, out dpiX, out dpiY);
                var scaleX = (double)dpiX / (double)DefaultDpi;
                var scaleY = (double)dpiY / (double)DefaultDpi;

                if (!IsLoaded && !workingArea.Contains(scaledRect))
                {
                    FitWindowRect(ref scaledRect, workingArea, scaleX, scaleY);
                    newRect.Size = new Size(scaledRect.Width / scaleX, scaledRect.Height / scaleY);
                    newRect.Location = new Point(scaledRect.X, scaledRect.Y);
                    _winRect = new Rect(scaledRect.X, scaledRect.Y, scaledRect.Width, scaledRect.Height);
                }

                AlignWindowRect(ref newRect, _winRect, workingArea, scaleX, scaleY);
            }

            _ignoreNextWindowLocationChange = true;
            this.MoveWindow(newRect.Left, newRect.Top, newRect.Width, newRect.Height);
        }

        private static void FitWindowRect(
            ref System.Drawing.Rectangle scaledRect,
            System.Drawing.Rectangle workingArea,
            double scaleX,
            double scaleY)
        {
            var padding = 15;
            var aspectRatio = (double)scaledRect.Width / (double)scaledRect.Height;

            // shrink it - trying to keep aspect ratio
            scaledRect.Width = Math.Min(scaledRect.Width, workingArea.Width - (int)(padding * 2.0 * scaleX));
            scaledRect.Height = (int)(scaledRect.Width / aspectRatio);

            scaledRect.Height = Math.Min(scaledRect.Height, workingArea.Height - (int)(padding * 2.0 * scaleY));
            scaledRect.Width = (int)(scaledRect.Height * aspectRatio);

            // adjust position so it's fully visible
            if (scaledRect.X < workingArea.X)
            {
                scaledRect.X = workingArea.X + padding;
            }
            if (scaledRect.Y < workingArea.Y)
            {
                scaledRect.Y = workingArea.Y + padding;
            }
            if (scaledRect.Right > workingArea.Right)
            {
                scaledRect.X = workingArea.Right - scaledRect.Width - padding;
            }
            if (scaledRect.Bottom > workingArea.Bottom)
            {
                scaledRect.Y = workingArea.Bottom - scaledRect.Height - padding;
            }
        }

        private static void AlignWindowRect(ref Rect newRect, Rect currRect, System.Drawing.Rectangle workingArea, double scaleX, double scaleY)
        {
            // This is what the more recent versions of MacOS seem to do:
            //
            // Align to an edge if remaining space between window (C) and
            // working area (screen minus taskbars) is < 1/4
            // I.e.
            //   if L < (L+R)/4 align left edge
            //   else if R < (L+R)/4 align right edge
            //   else center horizontally
            // similarly for vertial
            //
            // |---|-----------|---|
            // |   |     T     |   |
            // |---|-----------|---|
            // |   |           |   |
            // |L  |     C     | R |
            // |   |           |   |
            // |---|-----------|---|
            // |   |     B     |   |
            // |---|-----------|---|

            var lr = workingArea.Width - currRect.Width;
            var l = currRect.Left - workingArea.X;
            var r = workingArea.X + workingArea.Width - currRect.Right;
            if (l < lr / 4)
            {
                newRect.X = currRect.Left;
            }
            else if (r < lr / 4)
            {
                newRect.X = currRect.Right - newRect.Width * scaleX;
            }
            else
            {
                newRect.X = (currRect.Left + currRect.Width / 2) - newRect.Width / 2 * scaleX;
            }

            var tb = workingArea.Height - currRect.Height;
            var t = currRect.Top - workingArea.Y;
            var b = workingArea.Y + workingArea.Height - currRect.Bottom;
            if (t < tb / 4)
            {
                newRect.Y = currRect.Top;
            }
            else if (b < tb / 4)
            {
                newRect.Y = currRect.Bottom - newRect.Height * scaleY;
            }
            else
            {
                newRect.Y = (currRect.Top + currRect.Height / 2) - newRect.Height / 2 * scaleY;
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

            var scale = DisplayDeviceHelper.GetScaleFactorFromWindow(this);

            var limitPercentX = 0.1 * scale.Horizontal;
            var limitPercentY = 0.1 * scale.Vertical;

            // use absolute pixels for calculation
            var pxSize = new Size(scale.Horizontal * size.Width, scale.Vertical * size.Height);
            var pxOldRect = this.GetWindowRectInPixel();

            // scale to new size, maintain centre
            var pxNewRect = Rect.Inflate(pxOldRect,
                (pxSize.Width - pxOldRect.Width) / 2,
                (pxSize.Height - pxOldRect.Height) / 2);

            var desktopRect = WindowHelper.GetDesktopRectFromWindowInPixel(this);

            var leftLimit = desktopRect.Left + desktopRect.Width * limitPercentX;
            var rightLimit = desktopRect.Right - desktopRect.Width * limitPercentX;
            var topLimit = desktopRect.Top + desktopRect.Height * limitPercentY;
            var bottomLimit = desktopRect.Bottom - desktopRect.Height * limitPercentY;

            if (pxOldRect.Left < leftLimit && pxOldRect.Right < rightLimit) // L
                pxNewRect.Location = new Point(Math.Max(pxOldRect.Left, desktopRect.Left), pxNewRect.Top);
            else if (pxOldRect.Left > leftLimit && pxOldRect.Right > rightLimit) // R
                pxNewRect.Location = new Point(Math.Min(pxOldRect.Right, desktopRect.Right) - pxNewRect.Width, pxNewRect.Top);
            else // C, fix window boundary
                pxNewRect.Offset(
                    Math.Max(0, desktopRect.Left - pxNewRect.Left) + Math.Min(0, desktopRect.Right - pxNewRect.Right), 0);

            if (pxOldRect.Top < topLimit && pxOldRect.Bottom < bottomLimit) // T
                pxNewRect.Location = new Point(pxNewRect.Left, Math.Max(pxOldRect.Top, desktopRect.Top));
            else if (pxOldRect.Top > topLimit && pxOldRect.Bottom > bottomLimit) // B
                pxNewRect.Location = new Point(pxNewRect.Left,
                    Math.Min(pxOldRect.Bottom, desktopRect.Bottom) - pxNewRect.Height);
            else // C, fix window boundary
                pxNewRect.Offset(0,
                    Math.Max(0, desktopRect.Top - pxNewRect.Top) + Math.Min(0, desktopRect.Bottom - pxNewRect.Bottom));

            // return absolute location and relative size
            return new Rect(pxNewRect.Location, size);
        }

        private Rect ResizeAndCentreNewWindow(Size size)
        {
            var desktopRect = WindowHelper.GetCurrentDesktopRectInPixel();
            var scale = DisplayDeviceHelper.GetCurrentScaleFactor();
            var pxSize = new Size(scale.Horizontal * size.Width, scale.Vertical * size.Height);

            var pxLocation = new Point(
                desktopRect.X + (desktopRect.Width - pxSize.Width) / 2,
                desktopRect.Y + (desktopRect.Height - pxSize.Height) / 2);

            // return absolute location and relative size
            return new Rect(pxLocation, size);
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

            ContextObject.Reset();

            // assign monitor color profile
            ContextObject.ColorProfileName = DisplayDeviceHelper.GetMonitorColorProfileFromWindow(this);

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

            PositionWindow(newSize);

            if (Visibility != Visibility.Visible)
            {
                Dispatcher.BeginInvoke(new Action(() => this.BringToFront(Topmost)), DispatcherPriority.Render);
                Show();
            }

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

        private void updateWinRect()
        {
            _winRect = this.GetWindowRectInPixel();
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);

            if (_ignoreNextWindowLocationChange)
            {
                _ignoreNextWindowLocationChange = false;
            }
            else
            {
                updateWinRect();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (Plugin != null)
            {
                updateWinRect();
            }

            UnloadPlugin();
            busyDecorator.Dispose();

            base.OnClosing(e);

            ProcessHelper.PerformAggressiveGC();
        }

        [DllImport("User32.dll")]
        private static extern IntPtr
            MonitorFromPoint(System.Drawing.Point point, uint flag);

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("shcore.dll")]
        private static extern uint
            GetDpiForMonitor(IntPtr hMonitor, MonitorDpiType dpiType, out int dpiX, out int dpiY);

        private enum MonitorDpiType
        {
            EFFECTIVE_DPI = 0,
            ANGULAR_DPI = 1,
            RAW_DPI = 2
        }
    }
}