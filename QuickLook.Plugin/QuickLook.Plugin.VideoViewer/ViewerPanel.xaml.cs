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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickLook.Plugin.VideoViewer
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ViewerPanel : UserControl, IDisposable
    {
        private readonly ContextObject _context;

        private bool _wasPlaying;

        public ViewerPanel(ContextObject context)
        {
            InitializeComponent();

            _context = context;

            buttonPlayPause.MouseLeftButtonUp += TogglePlayPause;
            //buttonMute.MouseLeftButtonUp += (sender, e) =>
            //{
            //    mediaElement.IsMuted = false;
            //    buttonMute.Visibility = Visibility.Collapsed;
            //};
            buttonMute.MouseLeftButtonUp += (sender, e) => mediaElement.IsMuted = !mediaElement.IsMuted;
            buttonStop.MouseLeftButtonUp += (sender, e) => mediaElement.Stop();
            buttonBackward.MouseLeftButtonUp += (sender, e) => Seek(TimeSpan.FromSeconds(-10));
            buttonForward.MouseLeftButtonUp += (sender, e) => Seek(TimeSpan.FromSeconds(10));

            sliderProgress.PreviewMouseDown += (sender, e) =>
            {
                _wasPlaying = mediaElement.IsPlaying;
                mediaElement.Pause();
            };
            sliderProgress.PreviewMouseUp += (sender, e) =>
            {
                if (_wasPlaying) mediaElement.Play();
            };

            mediaElement.MediaFailed += ShowErrorNotification;
            /*mediaElement.MediaEnded += (s, e) =>
            {
                if (mediaElement.IsOpen)
                    if (!mediaElement.NaturalDuration.HasTimeSpan)
                    {
                        mediaElement.Stop();
                        mediaElement.Play();
                    }
            };*/

            PreviewMouseWheel += (sender, e) => ChangeVolume((double) e.Delta / 120 / 50);
        }

        public void Dispose()
        {
            try
            {
                mediaElement?.Stop();
                mediaElement?.Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            mediaElement = null;
        }

        private void ChangeVolume(double delta)
        {
            mediaElement.Volume += delta;
        }

        private void Seek(TimeSpan delta)
        {
            _wasPlaying = mediaElement.IsPlaying;
            mediaElement.Pause();

            mediaElement.Position = mediaElement.Position + delta;

            if (_wasPlaying) mediaElement.Play();
        }

        private void TogglePlayPause(object sender, MouseButtonEventArgs e)
        {
            if (mediaElement.IsPlaying)
            {
                mediaElement.Pause();
            }
            else
            {
                if (mediaElement.HasMediaEnded)
                    mediaElement.Stop();
                mediaElement.Play();
            }
        }

        [DebuggerNonUserCode]
        private void ShowErrorNotification(object sender, ExceptionRoutedEventArgs exceptionRoutedEventArgs)
        {
            _context.ShowNotification("", "An error occurred while loading the video.");
            mediaElement?.Close();

            Dispose();


            //throw new Exception("fallback to default viewer.");
        }

        public void LoadAndPlay(string path)
        {
            mediaElement.Source = new Uri(path);
            mediaElement.MediaOpened += (sender, e) => mediaElement.Volume = 0.2;
        }

        ~ViewerPanel()
        {
            GC.SuppressFinalize(this);
            Dispose();
        }
    }
}