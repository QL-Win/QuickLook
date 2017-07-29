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
            buttonBackward.MouseLeftButtonUp += (sender, e) => SeekBackward();
            buttonForward.MouseLeftButtonUp += (sender, e) => SeekForward();

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
            mediaElement.MediaEnded += (s, e) =>
            {
                if (!mediaElement.NaturalDuration.HasTimeSpan)
                {
                    mediaElement.Stop();
                    mediaElement.Play();
                }
            };
        }

        public void Dispose()
        {
            mediaElement?.Stop();
            mediaElement?.Dispose();
            mediaElement = null;
            Debug.WriteLine("dispose done");
        }

        private void ResumePlaying()
        {
            _wasPlaying = mediaElement.IsPlaying;
        }

        private void SeekBackward()
        {
            _wasPlaying = mediaElement.IsPlaying;
            mediaElement.Pause();

            var pos = mediaElement.Position;
            var delta = TimeSpan.FromSeconds(15);

            mediaElement.Position = pos < pos - delta ? TimeSpan.Zero : pos - delta;

            if (_wasPlaying) mediaElement.Play();
        }

        private void SeekForward()
        {
            _wasPlaying = mediaElement.IsPlaying;
            mediaElement.Pause();

            var pos = mediaElement.Position;
            var len = mediaElement.NaturalDuration.TimeSpan;
            var delta = TimeSpan.FromSeconds(15);

            mediaElement.Position = pos + delta > len ? len : pos + delta;

            if (_wasPlaying) mediaElement.Play();
        }

        private void TogglePlayPause(object sender, MouseButtonEventArgs e)
        {
            if (mediaElement.IsPlaying)
                mediaElement.Pause();
            else
                mediaElement.Play();
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
            mediaElement.MediaOpened += (sender, e) => mediaElement.IsMuted = true;
        }

        ~ViewerPanel()
        {
            GC.SuppressFinalize(this);
            Dispose();
        }
    }
}