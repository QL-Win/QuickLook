using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

            var ori = ImageFileHelper.GetOrientationFromExif(path);

            var bitmap = new BitmapImage();
            using (var fs = File.OpenRead(path))
            {
                bitmap.BeginInit();
                bitmap.StreamSource = fs;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.Rotation = ori == ImageFileHelper.ExifOrientation.Rotate90CW
                    ? Rotation.Rotate90
                    : ori == ImageFileHelper.ExifOrientation.Rotate270CW
                        ? Rotation.Rotate270
                        : Rotation.Rotate0;
                bitmap.EndInit();
            }
            viewPanelImage.Source = bitmap;

            Loaded += (sender, e) => { ZoomToFit(); };

            viewPanel.PreviewMouseWheel += ViewPanel_PreviewMouseWheel;

            viewPanel.PreviewMouseLeftButtonDown += ViewPanel_PreviewMouseLeftButtonDown;
            viewPanel.PreviewMouseMove += ViewPanel_PreviewMouseMove;
        }

        private void ViewPanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragInitPos = e.GetPosition(viewPanel);
            var temp = _dragInitPos.Value; // Point is a type value
            temp.Offset(viewPanel.HorizontalOffset, viewPanel.VerticalOffset);
            _dragInitPos = temp;
        }

        private void ViewPanel_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragInitPos.HasValue)
                return;

            if (e.LeftButton == MouseButtonState.Released)
            {
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
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                return;

            e.Handled = true;

            var newZoom = _zoomFactor + e.Delta / 120 * 0.1;

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