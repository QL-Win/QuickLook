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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ImageMagick;
using XamlAnimatedGif;

namespace QuickLook.Plugin.ImageViewer
{
    /// <summary>
    ///     Interaction logic for ImagePanel.xaml
    /// </summary>
    public partial class ImagePanel : UserControl
    {
        private Point? _dragInitPos;
        private double _minZoomFactor = 1d;
        private double _zoomFactor = 1d;

        public ImagePanel(string path)
        {
            InitializeComponent();

            LoadImage(path);

            Loaded += (sender, e) => { ZoomToFit(); };

            viewPanel.PreviewMouseWheel += ViewPanel_PreviewMouseWheel;

            viewPanel.MouseLeftButtonDown += ViewPanel_MouseLeftButtonDown;
            viewPanel.MouseMove += ViewPanel_MouseMove;
        }

        private void LoadImage(string path)
        {
            if (Path.GetExtension(path).ToLower() == ".gif")
                AnimationBehavior.SetSourceUri(viewPanelImage, new Uri(path));

            using (var image = new MagickImage(path))
            {
                image.Rotate(image.Orientation == OrientationType.RightTop
                    ? 90
                    : image.Orientation == OrientationType.BottomRight
                        ? 180
                        : image.Orientation == OrientationType.LeftBotom
                            ? 270
                            : 0);

                viewPanelImage.Source = image.ToBitmapSource();
            }
        }

        private void ViewPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.MouseDevice.Capture(viewPanel);

            _dragInitPos = e.GetPosition(viewPanel);
            var temp = _dragInitPos.Value; // Point is a type value
            temp.Offset(viewPanel.HorizontalOffset, viewPanel.VerticalOffset);
            _dragInitPos = temp;
        }

        private void ViewPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragInitPos.HasValue)
                return;

            if (e.LeftButton == MouseButtonState.Released)
            {
                e.MouseDevice.Capture(null);

                _dragInitPos = null;
                return;
            }

            e.Handled = true;

            var delta = _dragInitPos.Value - e.GetPosition(viewPanel);

            viewPanel.ScrollToHorizontalOffset(delta.X);
            viewPanel.ScrollToVerticalOffset(delta.Y);
        }

        private void ViewPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            {
                // normal scroll
                viewPanel.ScrollToVerticalOffset(viewPanel.VerticalOffset - e.Delta);

                return;
            }

            // zoom
            var newZoom = _zoomFactor + (double) e.Delta / 120 * 0.1;

            newZoom = Math.Max(newZoom, _minZoomFactor);
            newZoom = Math.Min(newZoom, 3);

            Zoom(newZoom, false);
        }

        private void ZoomToFit()
        {
            var factor = Math.Min(viewPanel.ActualWidth / viewPanelImage.Source.Width,
                viewPanel.ActualHeight / viewPanelImage.Source.Height);

            _minZoomFactor = factor;

            Zoom(factor, true);
        }

        private void Zoom(double factor, bool fromCenter)
        {
            _zoomFactor = factor;

            var position = fromCenter
                ? new Point(viewPanelImage.Source.Width / 2, viewPanelImage.Source.Height / 2)
                : Mouse.GetPosition(viewPanelImage);

            viewPanelImage.LayoutTransform = new ScaleTransform(factor, factor);

            viewPanel.InvalidateMeasure();

            // critical for calcuating offset
            viewPanel.ScrollToHorizontalOffset(0);
            viewPanel.ScrollToVerticalOffset(0);
            UpdateLayout();

            var offset = viewPanelImage.TranslatePoint(position, viewPanel) - Mouse.GetPosition(viewPanel);
            viewPanel.ScrollToHorizontalOffset(offset.X);
            viewPanel.ScrollToVerticalOffset(offset.Y);
            UpdateLayout();
        }
    }
}