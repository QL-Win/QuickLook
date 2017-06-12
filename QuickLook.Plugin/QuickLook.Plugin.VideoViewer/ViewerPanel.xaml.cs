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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FontAwesome.WPF;
using Unosquare.FFmpegMediaElement;

namespace QuickLook.Plugin.VideoViewer
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ViewerPanel : UserControl, IDisposable
    {
        private readonly ContextObject _context;

        public ViewerPanel(ContextObject context)
        {
            InitializeComponent();

            _context = context;

            buttonPlayPause.MouseLeftButtonUp += TogglePlayPause;
            buttonMute.MouseLeftButtonUp += (sender, e) =>
            {
                mediaElement.IsMuted = false;
                buttonMute.Visibility = Visibility.Collapsed;
            };

            mediaElement.PropertyChanged += ChangePlayPauseButton;
            mediaElement.MouseLeftButtonUp += TogglePlayPause;
            mediaElement.MediaErrored += ShowErrorNotification;
            mediaElement.MediaFailed += ShowErrorNotification;
        }

        public void Dispose()
        {
            mediaElement?.Dispose();
        }

        private void TogglePlayPause(object sender, MouseButtonEventArgs e)
        {
            if (mediaElement.IsPlaying)
                mediaElement.Pause();
            else
                mediaElement.Play();
        }

        private void ChangePlayPauseButton(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IsPlaying" && e.PropertyName != "HasMediaEnded")
                return;

            buttonPlayPause.Icon = mediaElement.IsPlaying
                ? FontAwesomeIcon.PauseCircleOutline
                : FontAwesomeIcon.PlayCircleOutline;
        }

        [DebuggerNonUserCode]
        private void ShowErrorNotification(object sender, MediaErrorRoutedEventArgs e)
        {
            _context.ShowNotification("", "An error occurred while loading the video.");
            mediaElement.Stop();

            Dispose();


            throw new Exception("fallback to default viewer.");
        }

        public void LoadAndPlay(string path)
        {
            mediaElement.Source = new Uri(path);
            mediaElement.IsMuted = true;
            mediaElement.Play();
        }

        ~ViewerPanel()
        {
            GC.SuppressFinalize(this);
            Dispose();
        }
    }
}