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
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Meta.Vlc;
using Meta.Vlc.Interop.Media;
using QuickLook.Annotations;
using QuickLook.ExtensionMethods;
using MediaState = Meta.Vlc.Interop.Media.MediaState;

namespace QuickLook.Plugin.VideoViewer
{
    /// <summary>
    ///     Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ViewerPanel : UserControl, IDisposable, INotifyPropertyChanged
    {
        private readonly ContextObject _context;

        private Uri _coverArt;
        private bool _hasAudio;
        private bool _hasEnded;
        private bool _hasVideo;
        private bool _isMuted;
        private bool _isPlaying;
        private bool _wasPlaying;

        public ViewerPanel(ContextObject context)
        {
            InitializeComponent();

            // apply global theme
            Resources.MergedDictionaries[0].MergedDictionaries.Clear();

            ShowViedoControlContainer(null, null);
            viewerPanel.PreviewMouseMove += ShowViedoControlContainer;

            mediaElement.PropertyChanged += PlayerPropertyChanged;
            mediaElement.StateChanged += PlayerStateChanged;

            _context = context;

            buttonPlayPause.Click += TogglePlayPause;
            buttonTime.Click += (sender, e) => buttonTime.Tag = (string) buttonTime.Tag == "Time" ? "Length" : "Time";
            buttonMute.Click += (sender, e) => volumeSliderLayer.Visibility = Visibility.Visible;
            volumeSliderLayer.MouseDown += (sender, e) => volumeSliderLayer.Visibility = Visibility.Collapsed;

            sliderProgress.PreviewMouseDown += (sender, e) =>
            {
                _wasPlaying = mediaElement.VlcMediaPlayer.IsPlaying;
                mediaElement.Pause();
            };
            sliderProgress.PreviewMouseUp += (sender, e) =>
            {
                if (_wasPlaying) mediaElement.Play();
            };

            /*mediaElement.MediaEnded += (s, e) =>
            {
                if (mediaElement.IsOpen)
                    if (!mediaElement.NaturalDuration.HasTimeSpan)
                    {
                        mediaElement.Stop();
                        mediaElement.Play();
                    }
            };*/

            PreviewMouseWheel += (sender, e) => ChangeVolume((double) e.Delta / 120 * 4);
        }

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (value == _isMuted) return;
                _isMuted = value;
                mediaElement.IsMute = value;
                OnPropertyChanged();
            }
        }

        public bool HasEnded
        {
            get => _hasEnded;
            private set
            {
                if (value == _hasEnded) return;
                _hasEnded = value;
                OnPropertyChanged();
            }
        }

        public bool HasAudio
        {
            get => _hasAudio;
            private set
            {
                if (value == _hasAudio) return;
                _hasAudio = value;
                OnPropertyChanged();
            }
        }

        public bool HasVideo
        {
            get => _hasVideo;
            private set
            {
                if (value == _hasVideo) return;
                _hasVideo = value;
                OnPropertyChanged();
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            private set
            {
                if (value == _isPlaying) return;
                _isPlaying = value;
                OnPropertyChanged();
            }
        }

        public Uri CoverArt
        {
            get => _coverArt;
            private set
            {
                if (value == _coverArt) return;
                if (value == null) return;
                _coverArt = value;
                OnPropertyChanged();
            }
        }

        public string LibVlcPath { get; } = VlcSettings.LibVlcPath;

        public string[] VlcOption { get; } = VlcSettings.VlcOptions;

        public void Dispose()
        {
            try
            {
                Task.Run(() =>
                {
                    mediaElement?.Dispose();
                    mediaElement = null;
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ShowViedoControlContainer(object sender, MouseEventArgs e)
        {
            var show = (Storyboard) videoControlContainer.FindResource("ShowControlStoryboard");
            if (videoControlContainer.Opacity == 0 || videoControlContainer.Opacity == 1)
                show.Begin();
        }

        private void AutoHideViedoControlContainer(object sender, EventArgs e)
        {
            if (!HasVideo)
                return;

            if (videoControlContainer.IsMouseOver)
                return;

            var hide = (Storyboard) videoControlContainer.FindResource("HideControlStoryboard");

            hide.Begin();
        }

        private void PlayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var prop = e.PropertyName;

            switch (prop)
            {
                case nameof(mediaElement.IsMute):
                    IsMuted = mediaElement.IsMute;
                    break;
            }
        }

        private void PlayerStateChanged(object sender, ObjectEventArgs<MediaState> e)
        {
            var state = e.Value;

            switch (state)
            {
                case MediaState.Opening:
                    HasVideo = mediaElement.VlcMediaPlayer.VideoTrackCount > 0;
                    HasAudio = mediaElement.VlcMediaPlayer.AudioTrackCount > 0;
                    break;
                case MediaState.Playing:
                    UpdateMeta();
                    DetermineTheme();
                    HasVideo = mediaElement.VlcMediaPlayer.VideoTrackCount > 0;
                    HasAudio = mediaElement.VlcMediaPlayer.AudioTrackCount > 0;
                    IsPlaying = true;
                    break;
                case MediaState.Paused:
                    IsPlaying = false;
                    break;
                case MediaState.Ended:
                    IsPlaying = false;
                    HasEnded = true;
                    break;
            }
        }

        private void UpdateMeta()
        {
            if (HasVideo)
                return;

            var path = mediaElement.VlcMediaPlayer.Media.GetMeta(MetaDataType.ArtworkUrl);
            if (!string.IsNullOrEmpty(path))
                CoverArt = new Uri(path);

            metaTitle.Text = mediaElement.VlcMediaPlayer.Media.GetMeta(MetaDataType.Title);
            metaArtists.Text = mediaElement.VlcMediaPlayer.Media.GetMeta(MetaDataType.Artist);
            metaAlbum.Text = mediaElement.VlcMediaPlayer.Media.GetMeta(MetaDataType.Album);

            metaArtists.Visibility = string.IsNullOrEmpty(metaArtists.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
            metaAlbum.Visibility = string.IsNullOrEmpty(metaAlbum.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void DetermineTheme()
        {
            if (HasVideo)
                return;

            if (CoverArt == null)
                return;

            var dark = false;
            using (var bitmap = new Bitmap(CoverArt.LocalPath))
            {
                dark = bitmap.IsDarkImage();
            }

            _context.UseDarkTheme = dark;
        }

        private void ChangeVolume(double delta)
        {
            IsMuted = false;

            var newVol = mediaElement.Volume + (int) delta;
            newVol = Math.Max(newVol, 0);
            newVol = Math.Min(newVol, 100);

            mediaElement.Volume = newVol;
        }

        private void TogglePlayPause(object sender, EventArgs e)
        {
            if (mediaElement.VlcMediaPlayer.IsPlaying)
            {
                mediaElement.Pause();
            }
            else
            {
                if (HasEnded)
                    mediaElement.Stop();
                mediaElement.Play();
            }
        }

        public void LoadAndPlay(string path)
        {
            mediaElement.LoadMediaWithOptions(path, ":avcodec-hw=dxva2");
            mediaElement.Volume = 50;

            mediaElement.Play();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}